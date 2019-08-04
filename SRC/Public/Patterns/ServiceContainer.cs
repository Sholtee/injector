/********************************************************************************
* ServiceContainer.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
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
        private /*readonly*/ ServiceCollection FEntries;

        private ServiceEntry Register(ServiceEntry entry)
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
        internal ServiceEntry Service(Type iface, Type implementation, Lifetime lifetime)
        {
            ServiceEntry entry = ProducibleServiceEntryFactory.CreateEntry(lifetime, iface, implementation);
            entry.CheckValid();

            return Register(entry);
        }

        internal ServiceEntry Factory(Type iface, Func<IInjector, Type, object> factory, Lifetime lifetime)
        {
            ServiceEntry entry = ProducibleServiceEntryFactory.CreateEntry(lifetime, iface, factory);
            return Register(entry);
        }

        internal ServiceEntry Lazy(Type iface, ITypeResolver implementation, Lifetime lifetime)
        {
            ServiceEntry entry = ProducibleServiceEntryFactory.CreateEntry(lifetime, iface, implementation);
            return Register(entry);
        }

        internal ServiceEntry Proxy(Type iface, Func<IInjector, Type, object, object> decorator)
        {
            ServiceEntry entry = FEntries.QueryEntry(iface);

            //
            // Service(), Factory(), Lazy()
            //

            if (entry.Factory != null)
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

        internal ServiceEntry Instance(Type iface, object instance, bool releaseOnDispose)
        { 
            if (!iface.IsInstanceOfType(instance))
                throw new InvalidOperationException(string.Format(Resources.NOT_ASSIGNABLE, iface, instance.GetType()));

            return Register(new InstanceServiceEntry(iface, instance, releaseOnDispose));
        }

        public static explicit operator ServiceCollection(ServiceContainer container) => container?.FEntries;
        #endregion

        #region IServiceContainer
        /// <summary>
        /// See <see cref="IServiceContainer"/>
        /// </summary>
        IServiceContainer IServiceContainer.Service(Type iface, Type implementation, Lifetime lifetime)
        {
            Service(iface, implementation, lifetime);
            return Self;
        }

        /// <summary>
        /// See <see cref="IServiceContainer"/>
        /// </summary>
        IServiceContainer IServiceContainer.Lazy(Type iface, ITypeResolver implementation, Lifetime lifetime)
        {
            Lazy(iface, implementation, lifetime);
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
        IReadOnlyCollection<Type> IServiceContainer.Entries => FEntries.Select(entry => entry.Interface).ToArray();

        /// <summary>
        /// See <see cref="IServiceContainer"/>
        /// </summary>
        IInjector IServiceContainer.CreateInjector() => Injector.Create(this);
        #endregion

        #region IQueryServiceInfo
        /// <summary>
        /// See <see cref="IQueryServiceInfo"/>
        /// </summary>
        IServiceInfo IQueryServiceInfo.QueryServiceInfo(Type iface) => FEntries.QueryEntry(iface);
        #endregion

        #region Protected
        protected ServiceContainer() : this(null)
        {
        }

        protected ServiceContainer(ServiceContainer parent) : base(parent)
        {
            FEntries = new ServiceCollection((ServiceCollection) parent);

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