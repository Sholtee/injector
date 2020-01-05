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
                .ListMembers(System.Reflection.TypeExtensions.GetMethods, includeNonPublic: true)
                .SingleOrDefault(m => m.Name.Equals(ifaceMethod.Name, StringComparison.Ordinal) && m.GetParameters().SequenceEqual(paramz, ParameterComparer.Instance));

            if (targetMethod == null)
            {
                var mme = new MissingMethodException(Resources.METHOD_NOT_SUPPORTED);
                mme.Data.Add(nameof(ifaceMethod), ifaceMethod);
                throw mme;
            }

            //
            // Ellenorizzuk h a metodus lathato e a legeneralando szerelvenyunk szamara.
            //

            Visibility.Check(targetMethod, AssemblyName);

            //
            // TResult IInterface.Foo<TGeneric>(T1 para1, ref T2 para2, out T3 para3, TGeneric para4) => Target.Foo(para1, ref para2, out para3, para4);
            //

            return DeclareMethod(ifaceMethod, forceInlining: true).WithExpressionBody
            (
                expressionBody: ArrowExpressionClause
                (
                    expression: InvokeMethod(targetMethod, TARGET, ifaceMethod.GetParameters().Select(para => para.Name).ToArray())
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
                obj.Attributes // IN, OUT, stb

                //
                // Parameter neve nem erdekel bennunket (azonos tipussal es attributumokkal ket parametert
                // azonosnak veszunk).
                //

            }.GetHashCode();

            public static ParameterComparer Instance { get; } = new ParameterComparer();
        }

        internal static MemberDeclarationSyntax GenerateDuckProperty(PropertyInfo ifaceProperty)
        {
            PropertyInfo targetProperty = typeof(TTarget)
                .ListMembers(System.Reflection.TypeExtensions.GetProperties, includeNonPublic: true)
                .SingleOrDefault(p => 
                    p.Name == ifaceProperty.Name && 
                    p.PropertyType == ifaceProperty.PropertyType &&

                    //
                    // Ha az interface tulajdonsaga irhato akkor targetnak is irhatonak kell lennie
                    // (kulomben mind1 h irhato e v sem).
                    //

                    (!ifaceProperty.CanWrite || p.CanWrite) &&
                    (!ifaceProperty.CanRead || p.CanRead) &&

                    //
                    // Indexer property-knel pedig meg kell egyezniuk az index parameterek
                    // sorrendjenek es tipusanak.
                    //

                    ifaceProperty.GetIndexParameters().Select(ip => ip.ParameterType).SequenceEqual(p.GetIndexParameters().Select(ip => ip.ParameterType)));

            if(targetProperty == null)
            {
                var mme = new MissingMethodException(Resources.PROPERTY_NOT_SUPPORTED);
                mme.Data.Add(nameof(ifaceProperty), ifaceProperty);
                throw mme;
            }

            //
            // Ellenorizzuk h a property lathato e a legeneralando szerelvenyunk szamara.
            //

            Visibility.Check(targetProperty, AssemblyName, checkGet: ifaceProperty.CanRead, checkSet: ifaceProperty.CanWrite);

            //
            // Ne a "targetProperty"-n hivjuk h akkor is jol mukodjunk ha az interface indexerenek
            // maskepp vannak elnvezve a parameterei.
            //

            ExpressionSyntax propertyAccess = PropertyAccessExpression(ifaceProperty, TARGET);

            //
            // Nem gond ha mondjuk az interface property-nek nincs gettere, akkor a "getBody"
            // figyelmen kivul lesz hagyva.
            //

            ArrowExpressionClauseSyntax 
                getBody = ArrowExpressionClause
                (
                    expression: propertyAccess
                ),
                setBody = ArrowExpressionClause
                (
                    expression: AssignmentExpression
                    (
                        kind: SyntaxKind.SimpleAssignmentExpression,
                        left: propertyAccess,
                        right: IdentifierName(Value)
                    )
                );

            return ifaceProperty.IsIndexer()
                ? DeclareIndexer
                (
                    property: ifaceProperty,
                    getBody: paramz => getBody,
                    setBody: paramz => setBody,
                    forceInlining: true
                )
                : (MemberDeclarationSyntax) DeclareProperty
                (
                    property: ifaceProperty,
                    getBody: getBody,
                    setBody: setBody,
                    forceInlining: true
                );
        }

        internal static EventDeclarationSyntax GenerateDuckEvent(EventInfo ifaceEvent)
        {
            EventInfo targetEvent = typeof(TTarget)
                .ListMembers(System.Reflection.TypeExtensions.GetEvents, includeNonPublic: true)
                .SingleOrDefault(ev => ev.Name == ifaceEvent.Name && ev.EventHandlerType == ifaceEvent.EventHandlerType);

            if (targetEvent == null)
            {
                var mme = new MissingMethodException(Resources.EVENT_NOT_SUPPORTED);
                mme.Data.Add(nameof(ifaceEvent), ifaceEvent);
                throw mme;
            }

            //
            // Ellenorizzuk h az esemeny lathato e a legeneralando szerelvenyunk szamara.
            //

            Visibility.Check(targetEvent, AssemblyName, checkAdd: ifaceEvent.AddMethod != null, checkRemove: ifaceEvent.RemoveMethod != null);

            return DeclareEvent
            (
                ifaceEvent, 
                addBody: ArrowExpressionClause
                (
                    expression: RegisterEvent(targetEvent, TARGET, add: true)
                ),
                removeBody: ArrowExpressionClause(RegisterEvent(targetEvent, TARGET, add: false)),
                forceInlining: true
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
                    .ListMembers(System.Reflection.TypeExtensions.GetMethods)
                    .Where(m => !m.IsSpecialName)
                    .Select(m => AggregateException(m, GenerateDuckMethod, exceptions))
            );  
            
            members.AddRange
            (
                interfaceType
                    .ListMembers(System.Reflection.TypeExtensions.GetProperties)
                    .Select(p => AggregateException(p, GenerateDuckProperty, exceptions))
            );

            members.AddRange
            (
                interfaceType
                    .ListMembers(System.Reflection.TypeExtensions.GetEvents)
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
                catch (MissingMethodException e)
                {
                    exs.Add(e);
                    return default(TResult);
                }
            }
        }

        public static string AssemblyName => $"{CreateType<TTarget>()}_{CreateType<TInterface>()}_Duck";
    }
}
