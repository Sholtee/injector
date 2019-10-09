/********************************************************************************
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

    //
    // TODO: Mukodjunk internal lathatosagu tagokra is.
    //

    internal static class DuckGenerator<TTarget, TInterface>
    {
        private const string TARGET = nameof(DuckBase<TTarget>.Target);

        private const BindingFlags BINDING_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy;

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
                .GetMethods(BINDING_FLAGS)
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
                    expression: Invoke(targetMethod, TARGET, ifaceMethod.GetParameters().Select(para => para.Name).ToArray())
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
            PropertyInfo targetProperty = typeof(TTarget).GetProperty(ifaceProperty.Name, BINDING_FLAGS);

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
                // Olvasasnal ugyanigy.
                //

                (ifaceProperty.CanRead && !targetProperty.CanRead) ||

                //
                // Indexer property-knel pedig meg kell egyezniuk az index parameterek
                // sorrendjenek es tipusanak.
                //

                !ifaceProperty.GetIndexParameters().Select(p => p.ParameterType).SequenceEqual(targetProperty.GetIndexParameters().Select(p => p.ParameterType))
            )
            {
                var mme = new MissingMethodException(Resources.PROPERTY_NOT_SUPPORTED);
                mme.Data.Add(nameof(ifaceProperty), ifaceProperty);
                throw mme;
            }

            ExpressionSyntax propertyAccess = PropertyAccessExpression(ifaceProperty, TARGET);

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

        internal static EventDeclarationSyntax GenerateDuckEvent(EventInfo ifaceEvent)
        {
            EventInfo targetEvent = typeof(TTarget).GetEvent(ifaceEvent.Name, BINDING_FLAGS);

            if (targetEvent == null)
            {
                var mme = new MissingMethodException(Resources.EVENT_NOT_SUPPORTED);
                mme.Data.Add(nameof(ifaceEvent), ifaceEvent);
                throw mme;
            }

            return DeclareEvent
            (
                ifaceEvent, 
                addBody: ArrowExpressionClause
                (
                    expression: RegisterEvent(targetEvent, TARGET, add: true)
                ),
                removeBody: ArrowExpressionClause(RegisterEvent(targetEvent, TARGET, add: false))
            );
        }

        public static ClassDeclarationSyntax GenerateDuckClass()
        {
            Type 
                interfaceType = typeof(TInterface),
                @base = typeof(DuckBase<TTarget>);

            Debug.Assert(interfaceType.IsInterface());

            ClassDeclarationSyntax cls = ClassDeclaration
            (
                identifier: GeneratedClassName
            )
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

            var exceptions = new List<Exception>();
            
            members.AddRange
            (
                interfaceType
                    .ListInterfaceMembers(System.Reflection.TypeExtensions.GetMethods)
                    .Where(m => !m.IsSpecialName)
                    .Select(m => AggregateException(m, GenerateDuckMethod, exceptions))
            );  
            
            members.AddRange
            (
                interfaceType
                    .ListInterfaceMembers(System.Reflection.TypeExtensions.GetProperties)
                    .Select(p => AggregateException(p, GenerateDuckProperty, exceptions))
            );

            members.AddRange
            (
                interfaceType
                    .ListInterfaceMembers(System.Reflection.TypeExtensions.GetEvents)
                    .Select(e => AggregateException(e, GenerateDuckEvent, exceptions))
            );

            //
            // Az osszes hibat visszaadjuk (ha voltak).
            //

            if (exceptions.Any()) throw exceptions.Count == 1 ? exceptions.Single() : new AggregateException(exceptions);

            return cls.WithMembers(List(members));

            TResult AggregateException<T, TResult>(T arg, Func<T, TResult> selector, ICollection<Exception> exs)
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
