/********************************************************************************
* DuckGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.DI.Internals
{
    using Properties;
    using static ProxyGeneratorBase;

    internal class DuckGenerator
    {
        private const string TARGET = "Target";

        public Type Target { get; }

        public DuckGenerator(Type target) => Target = target;

        internal MethodDeclarationSyntax GenerateDuckMethod(MethodInfo ifaceMethod)
        {
            IReadOnlyList<ParameterInfo> paramz = ifaceMethod.GetParameters();

            MethodInfo targetMethod = Target
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m => m.Name.Equals(ifaceMethod.Name, StringComparison.Ordinal) && m.GetParameters().SequenceEqual(paramz, new ParameterComparer()));

            if (targetMethod == null)
            {
                var mme = new MissingMethodException(Resources.METHOD_NOT_SUPPORTED);
                mme.Data.Add(nameof(ifaceMethod), ifaceMethod);
                throw mme;
            }

            //
            // TResult IInterface.Foo<TGeneric>(T1 para1, ref T2 para2, out T3 para3, TGeneric para4) => Target.Foo(para1, ref para2, out para3, para4)
            //

            return DeclareMethod(ifaceMethod).WithExpressionBody
            (
                expressionBody: ArrowExpressionClause
                (
                    expression: Invoke(targetMethod, TARGET)
                )
            );
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

        internal PropertyDeclarationSyntax GenerateDuckProperty(PropertyInfo ifaceProperty)
        {
            PropertyInfo targetProperty = Target.GetProperty(ifaceProperty.Name, BindingFlags.Instance | BindingFlags.Public);

            if 
            (
                //
                // Nincs ilyen nevvel v nem publikus.
                //

                targetProperty == null ||
                
                //
                // Tipusa nem megfelelo.
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

            MemberAccessExpressionSyntax accessProperty = MemberAccessExpression
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
                    expression: accessProperty
                ),
                setBody: ArrowExpressionClause
                (
                    expression: AssignmentExpression
                    (
                        kind: SyntaxKind.SimpleAssignmentExpression,
                        left: accessProperty,
                        right: IdentifierName(VALUE)
                    )
                )
            );
        }
    }
}
