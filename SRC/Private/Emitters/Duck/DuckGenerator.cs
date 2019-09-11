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

            MethodInfo targetMethod = Target.GetMethod
            (
                name: ifaceMethod.Name, 
                types: paramz.Select(param => param.ParameterType).ToArray()
            );

            bool valid;

            if (valid = targetMethod?.IsPublic == true)
            {
                IReadOnlyList<ParameterInfo> targetParamz = targetMethod.GetParameters();

                for (int i = 0; i < paramz.Count && valid; i++)
                    //
                    // In, Out, stb
                    //

                    valid = paramz[i].Attributes != targetParamz[i].Attributes;
            }

            if (!valid)
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
                    expression: Invoke(targetMethod, nameof(Target))
                )
            );
        }
    }
}
