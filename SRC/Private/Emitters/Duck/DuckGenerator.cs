﻿/********************************************************************************
* DuckGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.DI.Internals
{
    using Properties;
    using static ProxyGeneratorBase;

    internal static class DuckGenerator<TTarget, TInterface>
    {
        private const string TARGET = nameof(DuckBase<TTarget>.Target);

        internal static MethodDeclarationSyntax GenerateDuckMethod(MethodInfo ifaceMethod)
        {
            IReadOnlyList<ParameterInfo> paramz = ifaceMethod.GetParameters();

            //
            // Ne a "GetMethod(string, Type[])"-ot hasznaljuk mert az nem fogja megtalalni a nyilt
            // generikus metodusokat mivel:
            //
            // "interface IFoo {void Foo<T>();}" es "class Foo {void Foo<T>(){}}"
            //
            // eseten amennyiben Foo nem valositja meg IFoo-t a ket generikus "T" nem ugyanaz a tipus.
            //

            MethodInfo targetMethod = typeof(TTarget)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m => m.Name.Equals(ifaceMethod.Name, StringComparison.Ordinal) && m.GetParameters().SequenceEqual(paramz, new ParameterComparer()));

            if (targetMethod == null)
            {
                var mme = new MissingMethodException(Resources.METHOD_NOT_SUPPORTED);
                mme.Data.Add(nameof(ifaceMethod), ifaceMethod);
                throw mme;
            }

            //
            // TResult IInterface.Foo<TGeneric>(T1 para1, ref T2 para2, out T3 para3, TGeneric para4) => Target.Foo(para1, ref para2, out para3, para4);
            //

            return DeclareMethod(ifaceMethod).WithExpressionBody
            (
                expressionBody: ArrowExpressionClause
                (
                    expression: Invoke(targetMethod, TARGET)
                )
            )
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
        }

        private sealed class ParameterComparer : IEqualityComparer<ParameterInfo>
        {
            public bool Equals(ParameterInfo x, ParameterInfo y) => GetHashCode(x) == GetHashCode(y);

            public int GetHashCode(ParameterInfo obj) => new
            {
                //
                // Lasd GenericArgumentComparer
                //

                Name = obj.ParameterType.FullName ?? obj.ParameterType.Name,
                obj.Attributes
            }.GetHashCode();
        }

        internal static PropertyDeclarationSyntax GenerateDuckProperty(PropertyInfo ifaceProperty)
        {
            PropertyInfo targetProperty = typeof(TTarget).GetProperty(ifaceProperty.Name, BindingFlags.Instance | BindingFlags.Public);

            if 
            (
                //
                // Nincs ilyen nevvel v nem publikus.
                //

                targetProperty == null ||
                
                //
                // Tipusa nem megfelelo. Megjegyzendo h itt nem kell a metodusoknal latott tipusellenorzest
                // vegezni mert peldanynal sose lehet nyitott generikus property.
                //

                (targetProperty.PropertyType != ifaceProperty.PropertyType) ||

                //
                // Ha az interface tulajdonsaga irhato akkor targetnak is irhatonak kell lennie
                // (kulomben mind1 h irhato e v sem).
                //

                (ifaceProperty.CanWrite && !targetProperty.CanWrite) ||

                //
                // Olvasasnal ugyanigy
                //

                (ifaceProperty.CanRead && !targetProperty.CanRead)
            )
            {
                var mme = new MissingMethodException(Resources.PROPERTY_NOT_SUPPORTED);
                mme.Data.Add(nameof(ifaceProperty), ifaceProperty);
                throw mme;
            }

            MemberAccessExpressionSyntax propertyAccess = MemberAccessExpression
            (
                kind: SyntaxKind.SimpleMemberAccessExpression,
                expression: IdentifierName(TARGET),
                name: IdentifierName(ifaceProperty.Name)
            );

            //
            // Nem gond ha mondjuk az interface property-nek nincs gettere, akkor a "getBody"
            // figyelmen kivul lesz hagyva.
            //

            return DeclareProperty
            (
                property: ifaceProperty,
                getBody: ArrowExpressionClause
                (
                    expression: propertyAccess
                ),
                setBody: ArrowExpressionClause
                (
                    expression: AssignmentExpression
                    (
                        kind: SyntaxKind.SimpleAssignmentExpression,
                        left: propertyAccess,
                        right: IdentifierName(Value)
                    )
                )
            );
        }

        public static ClassDeclarationSyntax GenerateDuckClass()
        {
            Type 
                interfaceType = typeof(TInterface),
                @base = typeof(DuckBase<TTarget>);

            Debug.Assert(interfaceType.IsInterface());

            ClassDeclarationSyntax cls = ClassDeclaration(GeneratedClassName)
            .WithModifiers
            (
                modifiers: TokenList
                (
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.SealedKeyword)
                )
            )
            .WithBaseList
            (
                baseList: BaseList
                (
                    new[] { @base, interfaceType }.CreateList<Type, BaseTypeSyntax>(t => SimpleBaseType(CreateType(t)))
                )
            );

            List<MemberDeclarationSyntax> members = new List<MemberDeclarationSyntax>(new MemberDeclarationSyntax[]
            {
                DeclareCtor(@base.GetApplicableConstructor())
            });

            //
            // BindingFlags.FlattenHierarchy nem mukodik interface-ekre.
            //

            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public;

            var exceptions = new List<Exception>();
            
            members.AddRange
            (
                GetMethods(interfaceType).Select(m => AggregateException(m, GenerateDuckMethod, exceptions))
            );  
            
            members.AddRange
            (
                GetProperties(interfaceType).Select(p => AggregateException(p, GenerateDuckProperty, exceptions))
            );

            //
            // Az osszes hibat visszaadjuk (ha voltak).
            //

            if (exceptions.Any()) throw exceptions.Count == 1 ? exceptions.Single() : new AggregateException(exceptions);

            return cls.WithMembers(List(members));

            IReadOnlyList<MethodInfo> GetMethods(Type type) => type
                .GetMethods(bindingFlags)
                .Where(method => !method.IsSpecialName)
                .Concat
                (
                    type.GetInterfaces().SelectMany(GetMethods)
                )
                .Distinct()
                .ToArray();

            IReadOnlyList<PropertyInfo> GetProperties(Type type) => type
                .GetProperties(bindingFlags)
                .Concat
                (
                    type.GetInterfaces().SelectMany(GetProperties)
                )
                .Distinct()
                .ToArray();

            TResult AggregateException<T, TResult>(T arg, Func<T, TResult> selector, List<Exception> exs)
            {
                try
                {
                    return selector(arg);
                }
                catch (Exception e)
                {
                    exs.Add(e);
                    return default(TResult);
                }
            }
        }

        public static string AssemblyName => $"{CreateType<TTarget>()}_{CreateType<TInterface>()}_Duck";
    }
}
