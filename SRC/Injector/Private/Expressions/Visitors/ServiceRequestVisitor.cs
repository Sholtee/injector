/********************************************************************************
* ServiceRequestVisitor.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;

    /// <summary>
    /// Visits <see cref="IInjector.Get(Type, object?)"/>, <see cref="IInjector.TryGet(Type, object?)"/>, <see cref="IInjectorBasicExtensions.Get{TInterface}(Interfaces.IInjector, object?)"/> and <see cref="IInjectorBasicExtensions.TryGet{TInterface}(Interfaces.IInjector, object?)"/> invocations.
    /// </summary>
    internal abstract class ServiceRequestVisitor : ExpressionVisitor
    {
        private static readonly MethodInfo[]
            FInjectorGet = new[]
            {
                MethodInfoExtractor.Extract<IInjector>(static i => i.Get(null!, null)),
                MethodInfoExtractor.Extract<IInjector>(static i => i.TryGet(null!, null))
            },
            FGenericInjectorGet = new[]
            {
                MethodInfoExtractor.Extract<IInjector>(static i => i.Get<object>(null)).GetGenericMethodDefinition(),
                MethodInfoExtractor.Extract<IInjector>(static i => i.TryGet<object>(null)).GetGenericMethodDefinition()
            };

        protected abstract Expression VisitServiceRequest(MethodCallExpression request, Expression scope, Type iface, string? name);

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (!node.Method.IsGenericMethod)
            {
                if (Array.IndexOf(FInjectorGet, node.Method) >= 0 && node.Arguments[0] is ConstantExpression iface && node.Arguments[1] is ConstantExpression name)
                {
                    return VisitServiceRequest
                    (
                        node,
                        node.Object,
                        (Type) iface.Value,
                        (string?) name.Value
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

            return base.VisitMethodCall(node);
        }
    }
}
