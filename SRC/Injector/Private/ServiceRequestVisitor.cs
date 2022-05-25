/********************************************************************************
* ServiceRequestVisitor.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class ServiceRequestVisitor : ExpressionVisitor
    {
        private static readonly MethodInfo[]
            FInjectorGet = new[]
            {
                MethodInfoExtractor.Extract<IInjector>(i => i.Get(null!, null)),
                MethodInfoExtractor.Extract<IInjector>(i => i.TryGet(null!, null))
            },
            FGenericInjectorGet = new[] {
                MethodInfoExtractor.Extract<IInjector>(i => i.Get<object>(null)).GetGenericMethodDefinition(),
                MethodInfoExtractor.Extract<IInjector>(i => i.TryGet<object>(null)).GetGenericMethodDefinition()
            };

        public event Func<MethodCallExpression, Expression, Type, string?, Expression>? VisitServiceRequest;

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (VisitServiceRequest is not null)
            {
                if (!node.Method.IsGenericMethod)
                {
                    if (Array.IndexOf(FInjectorGet, node.Method) >= 0 && node.Arguments[0] is ConstantExpression iface && node.Arguments[1] is ConstantExpression name)
                    {
                        return VisitServiceRequest
                        (
                            node,
                            node.Object,
                            (Type)iface.Value,
                            (string?)name.Value
                        );
                    }
                }
                else
                {
                    if (Array.IndexOf(FGenericInjectorGet, node.Method.GetGenericMethodDefinition()) >= 0 && node.Arguments[1] is ConstantExpression name)
                    {
                        return VisitServiceRequest
                        (
                            node,
                            node.Arguments[0],
                            node.Method.GetGenericArguments()[0],
                            (string?) name.Value
                        );
                    }
                }
            }

            return base.VisitMethodCall(node);
        }
    }
}
