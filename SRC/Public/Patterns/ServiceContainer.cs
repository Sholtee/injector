/********************************************************************************
* ServiceContainer.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Solti.Utils.DI
{
    using Internals;
    using Properties;

    /// <summary>
    /// Implements the <see cref="IServiceContainer"/> interface.
    /// </summary>
    public class ServiceContainer : Composite<IServiceContainer>, IServiceContainer
    {
        #region Private
        private readonly Dictionary<Type, ContainerEntry> FEntries;

        private bool GetEntry(Type iface, out ContainerEntry entry) => FEntries.TryGetValue(iface, out entry);

        private ContainerEntry Register(ContainerEntry entry)
        {
            try
            {
                FEntries.Add(entry.Interface, entry);
                return entry;
            }
            catch (ArgumentException e)
            {
                throw new ServiceAlreadyRegisteredException(entry.Interface, e);
            }                      
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

        private static ConstructorInfo ValidateImplementation(Type iface, Type implementation)
        {
            if (!iface.IsInterfaceOf(implementation))
                throw new InvalidOperationException(string.Format(Resources.NOT_ASSIGNABLE, iface, implementation));

            return implementation.GetApplicableConstructor();
        }
        #endregion

        #region Internal
        internal ContainerEntry Service(Type iface, Type implementation, Lifetime? lifetime)
        {
            //
            // Ne a Resolver.GetFor()-ban validaljunk, h generikusoknal is lefusson az ellenorzes.
            //

            ConstructorInfo constructor = ValidateImplementation(iface, implementation);

            var entry = new ContainerEntry(iface, implementation, lifetime);

            //
            // Ha generikus interface-t regisztralunk akkor nem kell (nem is lehet) 
            // legyartani a factory-t.
            //

            if (!iface.IsGenericTypeDefinition)
                entry.Factory = Resolver.GetFor(constructor).ConvertToFactory();

            return Register(entry);
        }

        internal ContainerEntry Factory(Type iface, Func<IInjector, Type, object> factory, Lifetime? lifetime)
        {
            return Register(new ContainerEntry(iface, lifetime: lifetime)
            {
                Factory = factory
            });
        }

        internal ContainerEntry Lazy(Type iface, ITypeResolver implementation, Lifetime? lifetime)
        {         
            var entry = new ContainerEntry(iface, implementation, lifetime);

            //
            // Ha generikus interface-t regisztralunk akkor nem kell (nem is lehet) 
            // legyartani a factory-t.
            //
            
            if (!iface.IsGenericTypeDefinition)
            {
                var factory = new Lazy<Func<IInjector, Type, object>>
                (
                    () => Resolver.GetFor(ValidateImplementation(entry.Interface, entry.Implementation /*triggereli a resolvert*/)).ConvertToFactory(),
                    
                    //
                    // A container oroklodes maitt a Lazy<> megosztasra kerulhet tobb szal kozt is
                    // -> biztositsuk h csak egyszer legyen meghivva.
                    //
                    
                    LazyThreadSafetyMode.ExecutionAndPublication
                );

                entry.Factory = (injector, type) => factory.Value(injector, type);
            }

            return Register(entry);
        }

        internal ContainerEntry GetEntry(Type iface)
        {
            if (GetEntry(iface, out var entry)) return entry;
     
            //
            // Meg benne lehet generikus formaban.
            //

            if (!iface.IsGenericType || !GetEntry(iface.GetGenericTypeDefinition(), out var genericEntry))
                throw new NotSupportedException(string.Format(Resources.DEPENDENCY_NOT_FOUND, iface));

            //
            // Ha a bejegyzesnek van kezzel felvett [Factory()] factory fv-e (akar generikusnak is)
            // akkor nincs dolgunk, kulomben felvesszuk az uj tipizalt bejegyzest.
            //

            return genericEntry.Factory != null ? genericEntry : Service
            (
                iface,
                genericEntry.Implementation.MakeGenericType(iface.GetGenericArguments()),
                genericEntry.Lifetime
            );        
        }

        internal ContainerEntry Proxy(Type iface, Func<IInjector, Type, object, object> decorator)
        {
            ContainerEntry entry = GetEntry(iface);

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

        internal ContainerEntry Instance(Type iface, object instance, bool releaseOnDispose)
        { 
            if (!iface.IsInstanceOfType(instance))
                throw new InvalidOperationException(string.Format(Resources.NOT_ASSIGNABLE, iface, instance.GetType()));

            //
            // Ha kezelni kell a peldany elettartamat akkor innentol ugyanugy viselkedunk mint
            // egy mar legyartott Singleton eseten.
            //

            return Register(new ContainerEntry(iface, lifetime: releaseOnDispose ? Lifetime.Singleton : (Lifetime?) null)
            {
                Value = instance
            });
        }
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
        IReadOnlyCollection<Type> IServiceContainer.Entries => FEntries.Keys.ToArray();

        /// <summary>
        /// See <see cref="IServiceContainer"/>
        /// </summary>
        IInjector IServiceContainer.CreateInjector() => Injector.Create(this);
        #endregion

        #region IQueryServiceInfo
        /// <summary>
        /// See <see cref="IQueryServiceInfo"/>
        /// </summary>
        IServiceInfo IQueryServiceInfo.QueryServiceInfo(Type iface) => GetEntry(iface);
        #endregion

        #region Protected
        protected ServiceContainer() : this(null)
        {
        }

        protected ServiceContainer(ServiceContainer parent) : base(parent)
        {
            //
            // Ha van szulo akkor masoljuk a bejegyzeseit. A masolas mikentjet lasd az 
            // ContainerEntry implementaciojaban.
            //

            FEntries = new Dictionary<Type, ContainerEntry>
            (
                parent?
                    .FEntries
                    .Values
                    .ToDictionary
                    (
                        entry => entry.Interface,
                        entry => (ContainerEntry) entry.Clone()
                    )
                ?? new Dictionary<Type, ContainerEntry>(0)
            );

            Self = InterfaceProxy<IServiceContainer>.Chain(this, current => new ParameterValidatorProxy<IServiceContainer>(current));
        }

        protected override IServiceContainer CreateChild() => new ServiceContainer(this).Self;

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                foreach (IDisposable disposable in FEntries.Values)
                {
                    disposable.Dispose();
                }

                FEntries.Clear();
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