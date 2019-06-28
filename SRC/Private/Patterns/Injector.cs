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

    internal sealed class Injector : Disposable, IInjector
    {
        #region Private
        private readonly Stack<Type> FCurrentPath = new Stack<Type>();

        private /*readonly*/ ServiceCollection FServices;

        private Injector() => throw new NotSupportedException();

        private Injector(IEnumerable<ContainerEntry> inheritedServices)
        {
            FServices = new ServiceCollection(inheritedServices)
            {
                //
                // Beallitjuk a proxyt, majd felvesszuk sajat magunkat.
                //

                new ContainerEntry(typeof(IInjector))
                {
                    Value = InterfaceProxy<IInjector>.Chain(this, current => new ParameterValidatorProxy<IInjector>(current))
                }
            };
        }

        private IInjector Self => (IInjector) Get(typeof(IInjector));
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

                //
                // A bejegyzesnek mar legyartottnak, vagy legyarthatonak kell lennie.
                //

                ContainerEntry entry = FServices.QueryEntry(iface);

                Debug.Assert(entry.Value != null || entry.Factory != null);

                //
                // Singleton eletciklusnal az elso hivasnal le kell gyartani es menteni a peldanyt.
                //

                if (entry.Lifetime == Lifetime.Singleton && entry.Value == null)
                    entry.Value = entry.Factory(Self, iface);

                //
                // Elvileg jok vagyunk: Ha van "Value"-nk ("Singleton" eletciklus vagy Instance() hivas) 
                // akkor visszaadjuk azt, kulomben legyartjuk az uj peldanyt.
                //

                return entry.Value ?? entry.Factory(Self, iface);
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
