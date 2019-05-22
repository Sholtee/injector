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

    public class InterfaceProxy<TInterface>
    {
        //
        // DispatchProxy leszarmazottnak publikusnak kell lennie, nem lehet sealed es kell
        // rendelkezzen parameter nelkuli publikus konstruktorral.
        //

        public class Dispatcher : DispatchProxy
        {
            private InterfaceProxy<TInterface> Parent { get; set; }

            protected sealed override object Invoke(MethodInfo targetMethod, object[] args)
            {
                return Parent.Invoke(targetMethod, args);
            }

            public static TInterface Create(InterfaceProxy<TInterface> parent)
            {
                TInterface result = Create<TInterface, Dispatcher>();

                Dispatcher dispatcher = result as Dispatcher;
                Debug.Assert(dispatcher != null);

                dispatcher.Parent = parent;

                return result;
            }
        }

        public TInterface Proxy { get; }

        public TInterface Target { get; }

        public InterfaceProxy(TInterface target)
        {
            Target = target;
            Proxy  = Dispatcher.Create(this);
        }

        protected virtual object Invoke(MethodInfo targetMethod, object[] args)
        {
            return targetMethod.FastInvoke(Target, args);
        }
    }
}
