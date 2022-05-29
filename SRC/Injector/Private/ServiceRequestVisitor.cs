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

        private readonly Func<MethodCallExpression, Expression, Type, string?, Expression> FVisitor;

        public ServiceRequestVisitor(Func<MethodCallExpression, Expression, Type, string?, Expression> visitor) => FVisitor = visitor;

        public int VisitedRequests { get; private set; }

        public int AlteredRequests { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (!node.Method.IsGenericMethod)
            {
                if (Array.IndexOf(FInjectorGet, node.Method) >= 0 && node.Arguments[0] is ConstantExpression iface && node.Arguments[1] is ConstantExpression name)
                {
                    return CallVisitor
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
                    return CallVisitor
                    (
                        node,
                        node.Arguments[0],
                        node.Method.GetGenericArguments()[0],
                        (string?) name.Value
                    );
                }
            }

            return base.VisitMethodCall(node);

            Expression CallVisitor(MethodCallExpression original, Expression target, Type iface, string? name)
            {
                VisitedRequests++;

                Expression result = FVisitor(original, target, iface, name);
                if (result != original)
                    AlteredRequests++;

                return result;
            }
        }
    }
}
