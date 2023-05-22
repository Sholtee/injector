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
    // - NO System.Linq
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
            private InvocationContextEx(InvocationContext original, AspectAggregator<TInterface, TTarget> parent, int index) : base(original.Args, original)
            {
                Debug.Assert(parent is TInterface, "Got a proxy not implementing the service interface");
                Parent = parent;
                Index = index;
            }

            public InvocationContextEx(InvocationContext original, AspectAggregator<TInterface, TTarget> parent) : this(original, parent, 0)
            {
            }

            public object ProxyInstance => Parent;

            public AspectAggregator<TInterface, TTarget> Parent { get; }

            public int Index { get; }

            public IInvocationContext Next => Index < Parent.Interceptors.Count
                ? new InvocationContextEx(this, Parent, Index + 1)
                : throw new IndexOutOfRangeException();

            public object? InvokeInterceptor() => Index < Parent.Interceptors.Count
                ? Parent.Interceptors[Index].Invoke(this, static ctx => ctx.Next.InvokeInterceptor())
                : Parent.CallTarget(this);
        }

        private object? CallTarget(InvocationContext ctx) => base.Invoke(ctx);

        /// <summary>
        /// Creates a new <see cref="AspectAggregator{TInterface, TTarget}"/> instance.
        /// </summary>
        public AspectAggregator(TTarget target, params IInterfaceInterceptor[] interceptors) : base(target) =>
            Interceptors = interceptors ?? throw new ArgumentNullException(nameof(interceptors));

        /// <summary>
        /// Dispatches the invocation to the corresponding aspects
        /// </summary>
        public override object? Invoke(InvocationContext context) => new InvocationContextEx(context, this).InvokeInterceptor();

        /// <summary>
        /// Returns the bound interceptors.
        /// </summary>
        public IReadOnlyList<IInterfaceInterceptor> Interceptors { get; }
    }
}
