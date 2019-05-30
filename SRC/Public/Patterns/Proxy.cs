/********************************************************************************
* InterfaceProxy.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics;
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
        //
        // DispatchProxy leszarmazottnak publikusnak kell lennie, nem lehet sealed es kell
        // rendelkezzen parameter nelkuli publikus konstruktorral.
        //

        public class Dispatcher : DispatchProxy
        {
            private InterfaceProxy<TInterface> Parent { get; set; }

            protected sealed override object Invoke(MethodInfo targetMethod, object[] args) => Parent.Invoke(targetMethod, args);
           
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
        /// 
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
        protected virtual object Invoke(MethodInfo targetMethod, object[] args) => targetMethod.FastInvoke(Target, args);     
    }
}
