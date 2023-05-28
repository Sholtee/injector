/********************************************************************************
* AspectAggregator.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Proxy;

    //                                        !!!ATTENTION!!!
    //
    // This class is a critical component therefore every modification should be done carefully, with
    // performance in mind.
    // - NO System.Linq
    // - NO System.Reflection
    // - After ANY modifications, run the unit & performance tests to verify there is no regression
    //

    /// <summary>
    /// Aggregates <typeparamref name="TTarget"/> (class or interface) aspects to reduce the number of interceptors to be built.
    /// </summary>
    public class InterceptorAggregator<TInterface, TTarget> : InterfaceInterceptor<TInterface, TTarget>, IInterceptorAggregator where TTarget : class, TInterface where TInterface : class
    {
        private readonly IInterfaceInterceptor[] FInterceptors;

        /// <summary>
        /// Creates a new <see cref="InterceptorAggregator{TInterface, TTarget}"/> instance.
        /// </summary>
        public InterceptorAggregator(TTarget target, params IInterfaceInterceptor[] interceptors) : base(target) =>
            FInterceptors = interceptors ?? throw new ArgumentNullException(nameof(interceptors));

        /// <summary>
        /// Dispatches the invocation to the corresponding aspects
        /// </summary>
        public sealed override object? Invoke(InvocationContext context) => IInvocationContextFactory
            .Create(context, this)
            .InvokeInterceptor();

        /// <summary>
        /// Returns the bound interceptors.
        /// </summary>
        IInterfaceInterceptor[] IInterceptorAggregator.Interceptors => FInterceptors;

        object? IInterceptorAggregator.Target => Target;

        object? IInterceptorAggregator.CallTarget(InvocationContext ctx) => base.Invoke(ctx);
    }
}
