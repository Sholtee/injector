/********************************************************************************
* AspectAggregator.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Proxy;

    //                                        !!!ATTENTION!!!
    //
    // This class is a critical component therefore every modification should be done carefully, with
    // performance in mind.
    // - NO Sysmte.Linq
    // - NO System.Reflection
    // - After ANY modifications, run the unit & performance tests to verify there is no regression
    //

    /// <summary>
    /// Aggregates <typeparamref name="TTarget"/> (class or interface) aspects to reduce the number of interceptors to be built.
    /// </summary>
    public class AspectAggregator<TInterface, TTarget>: InterfaceInterceptor<TInterface, TTarget> where TTarget: class, TInterface where TInterface : class
    {
        private sealed class InvocationContextEx : InvocationContext, IInvocationContext
        {
            public InvocationContextEx(InvocationContext original, object proxyInstance) : base(original.Args, original)
            {
                Debug.Assert(proxyInstance is TInterface, "Got a proxy not implementing the service interface");
                ProxyInstance = proxyInstance;
            }

            public object ProxyInstance { get; }
        }

        private readonly IInterfaceInterceptor[] FInterceptors;

        private object? Invoke(InvocationContextEx ctx, int index) => index > 0
            ? FInterceptors[index - 1].Invoke(ctx, () => Invoke(ctx, index - 1))
            : base.Invoke(ctx);

        /// <summary>
        /// Creates a new <see cref="AspectAggregator{TInterface, TTarget}"/> instance.
        /// </summary>
        public AspectAggregator(TTarget target, params IInterfaceInterceptor[] interceptors) : base(target) =>
            FInterceptors = interceptors ?? throw new ArgumentNullException(nameof(interceptors));

        /// <summary>
        /// Dispatches the invocation to the corresponding aspects
        /// </summary>
        public override object? Invoke(InvocationContext context) => Invoke
        (
            new InvocationContextEx(context, this),
            FInterceptors.Length
        );

        /// <summary>
        /// Returns the bound interceptors.
        /// </summary>
        public IReadOnlyList<IInterfaceInterceptor> Interceptors => FInterceptors;
    }
}
