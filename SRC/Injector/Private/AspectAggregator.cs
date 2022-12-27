/********************************************************************************
* AspectAggregator.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Proxy;

    /// <summary>
    /// Aggregates <typeparamref name="TTarget"/> (class or interface) aspects to reduce the number of interceptors to be built.
    /// </summary>
    public class AspectAggregator<TInterface, TTarget>: InterfaceInterceptor<TInterface, TTarget> where TTarget: class, TInterface where TInterface : class
    {
        private sealed class InvocationContextEx : InvocationContext, IInvocationContext
        {
            public InvocationContextEx(InvocationContext original) : base(original.Args, original)
            {
            }
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
            new InvocationContextEx(context),
            FInterceptors.Length
        );

        /// <summary>
        /// Returns the bound interceptors.
        /// </summary>
        public IReadOnlyList<IInterfaceInterceptor> Interceptors => FInterceptors;
    }
}
