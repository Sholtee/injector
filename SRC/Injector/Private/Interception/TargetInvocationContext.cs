/********************************************************************************
* TargetInvocationContext.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Proxy;

    internal sealed class TargetInvocationContext: InterceptorInvocationContext
    {
        public TargetInvocationContext(InvocationContext original, IInterceptorAggregator parent) : base(original, parent)
        {
        }

        public override IInvocationContext? Next { get; }

        public override object? InvokeInterceptor() => Parent.CallTarget(Original);
    }
}
