/********************************************************************************
* InterfaceProxy.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.DI
{
    using Internals;

    /// <summary>
    /// Creates a transparent proxy against the given interface.
    /// </summary>
    /// <typeparam name="TInterface">The target interface.</typeparam>
    public class InterfaceProxy<TInterface>
    {
        /// <summary>
        /// Dispatches the calls to its parent.
        /// </summary>
        public class Dispatcher : DispatchProxy
        {
            //
            // DispatchProxy leszarmazottnak publikusnak kell lennie, nem lehet sealed es kell
            // rendelkezzen parameter nelkuli publikus konstruktorral.
            //

            private InterfaceProxy<TInterface> Parent { get; set; }

            protected sealed override object Invoke(MethodInfo targetMethod, object[] args) => Parent.Invoke(targetMethod, args);
           
            /// <summary>
            /// Creates a new proxy with the given parent.
            /// </summary>
            /// <param name="parent">The parent of this proxy.</param>
            /// <returns>The newly created proxy.</returns>
            public static TInterface Create(InterfaceProxy<TInterface> parent)
            {
                TInterface result = Create<TInterface, Dispatcher>();

                Dispatcher dispatcher = result as Dispatcher;
                Debug.Assert(dispatcher != null);

                dispatcher.Parent = parent;

                return result;
            }
        }

        /// <summary>
        /// The generated proxy.
        /// </summary>
        public TInterface Proxy { get; }

        /// <summary>
        /// The target, specified in the constructor.
        /// </summary>
        public TInterface Target { get; }

        /// <summary>
        /// Creates a new <see cref="InterfaceProxy{TInterface}"/> instance.
        /// </summary>
        /// <param name="target">The target of the proxy. By default it can not be null.</param>
        public InterfaceProxy(TInterface target)
        {
            Target = target;
            Proxy  = Dispatcher.Create(this);
        }

        /// <summary>
        /// Method calls on <see cref="Proxy"/> trigger this method. Without overriding, this method simply dispatches the call to the <see cref="Target"/>.
        /// </summary>
        /// <param name="targetMethod">The <see cref="MethodInfo"/> representing the called method.</param>
        /// <param name="args">Arguments passed to the call.</param>
        /// <returns>By default <see cref="Invoke"/> returns the value returned by the <see cref="Target"/> method call.</returns>
        protected virtual object Invoke(MethodInfo targetMethod, object[] args) => targetMethod.Call(Target, args);

        internal sealed class DynamicProxy
        {
            public TInterface Target { get; }

            public DynamicProxy(TInterface target) => Target = target;

            public InterfaceProxy<TInterface> Create<TProxy>() where TProxy : InterfaceProxy<TInterface>
                => (InterfaceProxy<TInterface>) typeof(TProxy).CreateInstance(new[] {typeof(TInterface)}, Target);
        }

        internal static TInterface Chain(TInterface seed, params Func<DynamicProxy, InterfaceProxy<TInterface>>[] proxies) =>
            proxies.Aggregate(seed, (current, proxy) => proxy(new DynamicProxy(current)).Proxy);
    }
}
