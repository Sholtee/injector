/********************************************************************************
* ServiceContainer.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Solti.Utils.DI
{
    using Internals;
    using Proxy;
    using Properties;

    /// <summary>
    /// Implements the <see cref="IServiceContainer"/> interface.
    /// </summary>
    public class ServiceContainer : Composite<IServiceContainer>, IServiceContainer
    {
        #region Private
        //
        // Singleton elettartamnal parhuzamosan is modositasra kerulhet a lista (generikus 
        // bejegyzes lezarasakor) ezert szalbiztosnak kell h legyen.
        //

        private /*readonly*/ ConcurrentServiceCollection FEntries;

        private AbstractServiceEntry Register(AbstractServiceEntry entry)
        {
            FEntries.Add(entry);
            return entry;
        }

        private static object TypeChecker(IInjector injector, Type type, object inst)
        {
            //
            // A letrhozott peldany tipusat ellenorizzuk. 
            //

            if (!type.IsInstanceOfType(inst))
                throw new Exception(string.Format(Resources.INVALID_INSTANCE, type));

            return inst;
        }
        #endregion

        #region Internal
        internal IReadOnlyCollection<AbstractServiceEntry> Entries => FEntries;

        internal AbstractServiceEntry Service(Type iface, Type implementation, Lifetime lifetime) => Register
        (
            ProducibleServiceEntryFactory.CreateEntry(lifetime, iface, implementation, FEntries)
        );

        internal AbstractServiceEntry Factory(Type iface, Func<IInjector, Type, object> factory, Lifetime lifetime) => Register
        (
            ProducibleServiceEntryFactory.CreateEntry(lifetime, iface, factory, FEntries)
        );

        internal AbstractServiceEntry Lazy(Type iface, ITypeResolver implementation, Lifetime lifetime) => Register
        (
            ProducibleServiceEntryFactory.CreateEntry(lifetime, iface, implementation, FEntries)
        );

        internal AbstractServiceEntry Proxy(Type iface, Func<IInjector, Type, object, object> decorator)
        {
            AbstractServiceEntry entry = FEntries.Query(iface);

            //
            // Service(), Factory(), Lazy()
            //

            if (entry.Owner == FEntries && entry.Factory != null)
            {
                Func<IInjector, Type, object> oldFactory = entry.Factory;

                entry.Factory = (injector, type) => decorator(injector, type, oldFactory(injector, type));
                return entry;
            }

            //
            // Generikus szerviz es Instance() eseten a metodus nem ertelmezett.
            //

            throw new InvalidOperationException(Resources.CANT_PROXY);
        }

        internal AbstractServiceEntry Instance(Type iface, object instance, bool releaseOnDispose) => Register(new InstanceServiceEntry(iface, instance, releaseOnDispose, FEntries));
        #endregion

        #region IServiceContainer
        /// <summary>
        /// See <see cref="IServiceContainer"/>
        /// </summary>
        IServiceContainer IServiceContainer.Service(Type iface, Type implementation, Lifetime lifetime)
        {
            //
            // Implementacio validalas (mukodik generikusokra is).
            //

            if (!iface.IsInterfaceOf(implementation))
                throw new InvalidOperationException(string.Format(Resources.NOT_ASSIGNABLE, iface, implementation));

            //
            // "implementation.GetApplicableConstructor()" hivassal itt ellenorizhetnenk a
            // konstruktort is (mukodne generikusokra is) de h a Lazy() hivashoz hasonloan
            // mukodjunk ezt kihagyjuk es peldanyositaskor validalunk.
            //

            Service(iface, implementation, lifetime);
            return Self;
        }

        /// <summary>
        /// See <see cref="IServiceContainer"/>
        /// </summary>
        IServiceContainer IServiceContainer.Lazy(Type iface, ITypeResolver implementation, Lifetime lifetime)
        {
            Lazy(iface, implementation, lifetime);

            //
            // Ha nem generikusunk van (van Factory) akkor a gyarnak tipus validaltnak kell
            // lennie.
            //

            if (!iface.IsGenericTypeDefinition()) Proxy(iface, TypeChecker);

            return Self;
        }

        /// <summary>
        /// See <see cref="IServiceContainer"/>
        /// </summary>
        IServiceContainer IServiceContainer.Factory(Type iface, Func<IInjector, Type, object> factory, Lifetime lifetime)
        {
            Factory(iface, factory, lifetime);

            //
            // A visszaadott peldany tipusat meg ellenorizni kell.
            //

            Proxy(iface, TypeChecker);

            return Self;
        }

        /// <summary>
        /// See <see cref="IServiceContainer"/>
        /// </summary>
        IServiceContainer IServiceContainer.Instance(Type iface, object instance, bool releaseOnDispose)
        {
            if (!iface.IsInstanceOfType(instance))
                throw new InvalidOperationException(string.Format(Resources.NOT_ASSIGNABLE, iface, instance.GetType()));

            Instance(iface, instance, releaseOnDispose);

            return Self;
        }

        /// <summary>
        /// See <see cref="IServiceContainer"/>
        /// </summary>
        IServiceContainer IServiceContainer.Proxy(Type iface, Func<IInjector, Type, object, object> decorator)
        {
            Proxy(iface, decorator);

            //
            // A visszaadott peldany tipusat meg ellenorizni kell.
            //

            Proxy(iface, TypeChecker);

            return Self;
        }

        /// <summary>
        /// See <see cref="IServiceContainer"/>
        /// </summary>
        IInjector IServiceContainer.CreateInjector() => Injector.Create(Entries);
        #endregion

        #region IQueryServiceInfo
        /// <summary>
        /// See <see cref="IQueryServiceInfo"/>
        /// </summary>
        IServiceInfo IQueryServiceInfo.QueryServiceInfo(Type iface) => FEntries.Query(iface);

        /// <summary>
        /// See <see cref="IQueryServiceInfo"/>
        /// </summary>
        IReadOnlyCollection<IServiceInfo> IQueryServiceInfo.Entries => FEntries;
        #endregion

        #region Protected
        protected ServiceContainer() : this(null)
        {
        }

        protected ServiceContainer(ServiceContainer parent) : base(parent)
        {
            FEntries = new ConcurrentServiceCollection(parent?.Entries);

            Self = ProxyUtils.Chain<IServiceContainer>(this, ProxyFactory.Create<IServiceContainer, ParameterValidatorProxy<IServiceContainer>>);
        }

        protected override IServiceContainer CreateChild() => new ServiceContainer(this).Self;

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                FEntries.Dispose();
                FEntries = null;
            }

            base.Dispose(disposeManaged);
        }

        protected override IServiceContainer Self { get; }
        #endregion

        /// <summary>
        /// Creates a new <see cref="IServiceContainer"/> instance.
        /// </summary>
        /// <returns>The newly created instance.</returns>
        public static IServiceContainer Create() => new ServiceContainer().Self;
    }
}