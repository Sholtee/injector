/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    using Properties;
    using Proxy;

    internal sealed class Injector : Disposable, IInjector
    {
        #region Private
        private readonly Stack<Type> FCurrentPath = new Stack<Type>();

        private /*readonly*/ ServiceCollection FServices;

        private Injector() => throw new NotSupportedException();

        private Injector(IEnumerable<ServiceEntry> inheritedServices)
        {
            FServices = new ServiceCollection(inheritedServices)
            {
                //
                // Beallitjuk a proxyt, majd felvesszuk sajat magunkat.
                //

                new InstanceServiceEntry
                (
                    typeof(IInjector),
                    
                    //
                    // "target" kell h a megfelelo overload-ot hivjuk
                    //

                    Self = ProxyUtils.Chain<IInjector>(this, me => ProxyFactory.Create<IInjector, ParameterValidatorProxy<IInjector>>(target: me)),
                    releaseOnDispose: false
                )
            };
        }

        private IInjector Self { get; }
        #endregion

        #region Protected
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                FServices.Dispose();
                FServices = null;
            }

            base.Dispose(disposeManaged);
        }
        #endregion

        #region Internals
        internal object Get(Type iface)
        {
            FCurrentPath.Push(iface);
            try
            {
                //
                // Ha egynel tobbszor szerepel az aktualis interface akkor korkoros referenciank van.
                //

                if (FCurrentPath.Count(t => t == iface) > 1)
                    throw new InvalidOperationException(string.Format(Resources.CIRCULAR_REFERENCE, string.Join(" -> ", FCurrentPath)));

                IServiceFactory factory = FServices.QueryEntry(iface);
                return factory.GetService(Self, iface);
            }
            finally
            {
                Type removed = FCurrentPath.Pop();
                Debug.Assert(removed == iface);
            }
        }

        internal static IInjector Create(ServiceContainer parentContainer) => new Injector((ServiceCollection) parentContainer).Self;
        #endregion

        #region IInjector
        /// <summary>
        /// See <see cref="IInjector"/>
        /// </summary>
        object IInjector.Get(Type iface) => Get(iface);

        /// <summary>
        /// See <see cref="IInjector"/>
        /// </summary>
        object IInjector.Instantiate(Type @class, IReadOnlyDictionary<string, object> explicitArgs) => Resolver.GetExtended(@class)(Self, explicitArgs ?? new Dictionary<string, object>(0));
        #endregion

        #region IQueryServiceInfo
        IServiceInfo IQueryServiceInfo.QueryServiceInfo(Type iface) => FServices.QueryEntry(iface);
        #endregion
    }
}
