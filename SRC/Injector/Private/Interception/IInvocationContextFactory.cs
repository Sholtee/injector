/********************************************************************************
* IInvocationContextFactory.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Proxy;

    internal static class IInvocationContextFactory
    {
        public static IInvocationContext Create(InvocationContext original, IInterceptorAggregator parent, int index) => index < parent.Interceptors.Length
            ? new InterceptorInvocationContext(original, parent, index)
            : new TargetInvocationContext(original, parent);

        public static IInvocationContext Create(InvocationContext original, IInterceptorAggregator parent) => Create(original, parent, 0);
    }
}
