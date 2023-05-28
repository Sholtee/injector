/********************************************************************************
* TargetInvocationContext.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI.Internals
{
    using Proxy;

    internal sealed class TargetInvocationContext: InterceptorInvocationContext
    {
        public TargetInvocationContext(InvocationContext original, IInterceptorAggregator parent) : base(original, parent)
        {
        }

        public override object? InvokeInterceptor() => Parent.CallTarget(Original);
    }
}
