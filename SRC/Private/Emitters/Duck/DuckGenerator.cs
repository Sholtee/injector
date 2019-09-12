/********************************************************************************
* DuckGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.DI.Internals
{
    using Properties;
    using static ProxyGeneratorBase;

    internal class DuckGenerator
    {
        public Type Target { get; }

        public DuckGenerator(Type target) => Target = target;

        public MethodDeclarationSyntax GenerateDuckMethod(MethodInfo ifaceMethod)
        {
            IReadOnlyList<ParameterInfo> paramz = ifaceMethod.GetParameters();

            MethodInfo targetMethod = Target
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m => m.GetParameters().SequenceEqual(paramz, new ParameterComparer()));

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
                    expression: Invoke(targetMethod, nameof(Target))
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

                Name = obj.ParameterType.Name ?? obj.ParameterType.FullName,
                obj.Attributes
            }.GetHashCode();
        }
    }
}
