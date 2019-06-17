/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Solti.Utils.DI
{
    using Properties;
    using Internals;
    using Annotations;

    /// <summary>
    /// Implements the <see cref="IInjector"/> interface.
    /// </summary>
    /// <remarks>You can not instantiate this class directly, use the static <see cref="Create"/> method instead.</remarks>
    public sealed class Injector : Composite<IInjector>, IInjector
    {
        #region Private
        private /*readonly*/ IDictionary<Type, InjectorEntry> FEntries;

        private /*readonly*/ ThreadLocal<ThreadContext> FContext;

        private bool FLocked;

        private Stack<Type> CurrentPath => FContext.Value.CurrentPath;

        private Injector(): this(null)
        {
        }

        private Injector(Injector parent): base(parent)
        {
            //
            // Ha van szulo akkor a sajat magat tartalmazo bejegyzesen kivul 
            // az osszes tobbi bejegyzest masoljuk.
            //
            // A masolas mikentjet lasd az InjectorEntry implementaciojaban.
            //

            FEntries = new ConcurrentDictionary<Type, InjectorEntry>(parent?
                .FEntries
                .Values
                .Where(entry => entry.Interface != typeof(IInjector))
                .ToDictionary
                (
                    entry => entry.Interface,
                    entry => (InjectorEntry) entry.Clone()
                ) ?? new Dictionary<Type, InjectorEntry>(0));

            FContext = new ThreadLocal<ThreadContext>(() => new ThreadContext(), trackAllValues: false);

            //
            // Beallitjuk a proxykat, majd felvesszuk sajat magunkat.
            //

            Instance
            (
                typeof(IInjector), 
                InterfaceProxy<IInjector>.Chain
                (
                    this, 
                    current => new ParameterValidatorProxy<IInjector>(current), 
                    current => new StateValidatorProxy<IInjector>(current)
                ), 
                releaseOnDispose: false
            );
        }

        private bool GetEntry(Type iface, out InjectorEntry entry) => FEntries.TryGetValue(iface, out entry);

        private InjectorEntry Register(InjectorEntry entry)
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

            //
            // Az implementacionak pontosan egy (megjelolt) konstruktoranak kell lennie.
            //

            try
            {
                IReadOnlyList<ConstructorInfo> constructors = implementation.GetConstructors();

                return constructors.SingleOrDefault(ctor => ctor.GetCustomAttribute<ServiceActivatorAttribute>() != null) ?? constructors.Single();
            }
            catch (InvalidOperationException)
            {
                throw new NotSupportedException(string.Format(Resources.CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED, implementation));
            }
        }
        #endregion

        #region Internal
        internal InjectorEntry Service(Type iface, Type implementation, Lifetime? lifetime)
        {
            //
            // Ne a Resolver.Create()-ban validaljunk, h generikusoknal is lefusson az ellenorzes.
            //

            ConstructorInfo constructor = ValidateImplementation(iface, implementation);

            //
            // Bejegyzes felvetele.
            //

            return Register(new InjectorEntry(iface, implementation, lifetime)
            {
                //
                // Ha generikus interface-t regisztralunk akkor nem kell (nem is lehet) 
                // legyartani a factory-t.
                //

                Factory = !iface.IsGenericTypeDefinition ? Resolver.Create(constructor) : null
            });
        }

        internal InjectorEntry Factory(Type iface, Func<IInjector, Type, object> factory, Lifetime? lifetime)
        {
            return Register(new InjectorEntry(iface, lifetime: lifetime)
            {
                Factory = factory
            });
        }

        internal InjectorEntry Lazy(Type iface, ITypeResolver implementation, Lifetime? lifetime)
        {         
            var entry = new InjectorEntry(iface, implementation, lifetime);

            //
            // Ha generikus interface-t regisztralunk akkor nem kell (nem is lehet) 
            // legyartani a factory-t.
            //
            
            if (!iface.IsGenericTypeDefinition)
            {
                Lazy<Func<IInjector, Type, object>> factory = new Lazy<Func<IInjector, Type, object>>
                (
                    () =>
                    {
                        ConstructorInfo constructor = ValidateImplementation(entry.Interface, entry.Implementation /*triggereli a resolvert*/);
                        return Resolver.Create(constructor);
                    }, 
                    LazyThreadSafetyMode.ExecutionAndPublication
                );

                entry.Factory = (injector, type) => factory.Value(injector, type);
            }

            return Register(entry);
        }

        internal InjectorEntry GetEntry(Type iface)
        {
            if (GetEntry(iface, out var entry)) return entry;
     
            //
            // Meg benne lehet generikus formaban.
            //

            if (!iface.IsGenericType || !GetEntry(iface.GetGenericTypeDefinition(), out var genericEntry))
                throw new NotSupportedException(string.Format(Resources.DEPENDENCY_NOT_FOUND, iface));

            //
            // Ha a bejegyzesnek van kezzel felvett [Factory()] factory fv-e (akar generikusnak is)
            // akkor nincs dolgunk.
            //

            if (genericEntry.Factory != null) return genericEntry;

            //
            // Kulomben vegyuk fel az uj tipizalt bejegyzest. 
            //
            // Megjegyzendo h 
            //   1) A MakeGenericType() es Service() hivas idoigenyes hivasok, ez van sprite.
            //   2) Jo otletnek tunhet lock()-t hasznalni h ha parhuzamosan ide jut ket szal is akkor
            //      ne kelljen kivetelt kezelni (ahogy korabban volt is):
            //
            //            lock(iface | genericEntry) return GetEntry(iface, out entry)
            //               ? entry
            //               : Service(...)
            //
            //      Az "iface" kivulrol is elerheto ezert azon nem lock-olnuk, a "genericEntry" hasznalata
            //      pedig lassithatja a lekerdezest (gondoljunk egy rendszerre ahol a generikus szolgaltatas 
            //      vmi Repository<T> es elofordul h parhuzasmosan a tobb modul is kulombozo repokat igenyel,
            //      ekkor a lock kvazi sorositana ezeket a kereseket [a lenti megoldas nem]).
            //

            try
            {
                return Service
                (
                    iface, 
                    genericEntry.Implementation.MakeGenericType(iface.GetGenericArguments()), 
                    genericEntry.Lifetime
                );
            }
            catch (ServiceAlreadyRegisteredException)
            {
                //
                // Opi, vki mar kozben regisztralta.
                //

                return GetEntry(iface);
            }         
        }

        internal InjectorEntry Proxy(Type iface, Func<IInjector, Type, object, object> decorator)
        {
            InjectorEntry entry = GetEntry(iface);

            //
            // Lock igazabol nem is kene, csak kis paranoia.
            //

            lock (entry)
            {
                //
                // Service(), Factory(), Lazy()
                //

                if (entry.Factory != null)
                {
                    Func<IInjector, Type, object> oldFactory = entry.Factory;

                    entry.Factory = (injector, type) => decorator(injector, type, oldFactory(injector, type));
                    return entry;
                }
            }

            //
            // Generikus szerviz es Instance() eseten a metodus nem ertelmezett.
            //

            throw new InvalidOperationException(Resources.CANT_PROXY);
        }

        internal InjectorEntry Instance(Type iface, object instance, bool releaseOnDispose)
        { 
            if (!iface.IsInstanceOfType(instance))
                throw new InvalidOperationException(string.Format(Resources.NOT_ASSIGNABLE, iface, instance.GetType()));

            //
            // Ha kezelni kell a peldany elettartamat akkor innentol ugyanugy viselkedunk mint
            // egy mar legyartott Singleton eseten.
            //

            return Register(new InjectorEntry(iface, lifetime: releaseOnDispose ? Lifetime.Singleton : (Lifetime?) null)
            {
                Value = instance
            });
        }

        internal object Get(Type iface)
        {
            CurrentPath.Push(iface);
            try
            {
                //
                // Ha egynel tobbszor szerepel az aktualis interface akkor korkoros referenciank van.
                //
                
                if (CurrentPath.Count(t => t == iface) > 1)
                    throw new InvalidOperationException(string.Format(Resources.CIRCULAR_REFERENCE, string.Join(" -> ", CurrentPath)));

                //
                // Bejegyzes lekerdezes.
                //

                InjectorEntry entry = GetEntry(iface);

                Debug.Assert(entry.Value != null || entry.Factory != null);

                if (entry.Lifetime == Lifetime.Singleton && entry.Value == null)
                    //
                    // Ha singleton eletciklusunk van es eddig meg nem volt akkor le kell 
                    // gyartanunk a peldanyt.
                    //

                    lock (entry)
                        if (entry.Value == null)
                            return entry.Value = entry.Factory(Self, iface);

                //
                // Elvileg jok vagyunk: Ha van "Value"-nk ("Singleton" eletciklus vagy Instance() hivas) 
                // akkor visszaadjuk azt, kulomben legyartjuk az uj peldanyt.
                //

                return entry.Value ?? entry.Factory(Self, iface);
            }
            finally
            {
                Type removed = CurrentPath.Pop();
                Debug.Assert(removed == iface);
            }
        }
        #endregion

        #region IInjector
        /// <summary>
        /// See <see cref="IInjector"/>
        /// </summary>
        IInjector IInjector.Service(Type iface, Type implementation, Lifetime lifetime)
        {
            Service(iface, implementation, lifetime);
            return Self;
        }

        /// <summary>
        /// See <see cref="IInjector"/>
        /// </summary>
        IInjector IInjector.Lazy(Type iface, ITypeResolver implementation, Lifetime lifetime)
        {
            Lazy(iface, implementation, lifetime);
            return Self;
        }

        /// <summary>
        /// See <see cref="IInjector"/>
        /// </summary>
        IInjector IInjector.Factory(Type iface, Func<IInjector, Type, object> factory, Lifetime lifetime)
        {
            Factory(iface, factory, lifetime);

            //
            // A visszaadott peldany tipusat meg ellenorizni kell.
            //

            Proxy(iface, TypeChecker);

            return Self;
        }

        /// <summary>
        /// See <see cref="IInjector"/>
        /// </summary>
        IInjector IInjector.Instance(Type iface, object instance, bool releaseOnDispose)
        {
            Instance(iface, instance, releaseOnDispose);
            return Self;
        }

        /// <summary>
        /// See <see cref="IInjector"/>
        /// </summary>
        IInjector IInjector.Proxy(Type iface, Func<IInjector, Type, object, object> decorator)
        {
            Proxy(iface, decorator);

            //
            // A visszaadott peldany tipusat meg ellenorizni kell.
            //

            Proxy(iface, TypeChecker);

            return Self;
        }

        /// <summary>
        /// See <see cref="IInjector"/>
        /// </summary>
        object IInjector.Get(Type iface) => Get(iface);

        /// <summary>
        /// See <see cref="IInjector"/>
        /// </summary>
        IReadOnlyCollection<Type> IInjector.Entries => FEntries.Keys.ToArray();

        /// <summary>
        /// See <see cref="IInjector"/>
        /// </summary>
        IServiceInfo IInjector.QueryServiceInfo(Type iface) => GetEntry(iface);

        /// <summary>
        /// See <see cref="ILockable"/>
        /// </summary>
        bool ILockable.Locked => FLocked;

        /// <summary>
        /// See <see cref="ILockable"/>
        /// </summary>
        void ILockable.Lock() => FLocked = true;
        #endregion

        #region Protected
        protected override IInjector CreateChild() => new Injector(this).Self;

        protected override void Dispose(bool disposeManaged)
        {
            //
            // Elso helyen szerepeljen h a Self meg mukodjon az osben is.
            //

            base.Dispose(disposeManaged);

            if (disposeManaged)
            {
                FContext.Dispose();
                FContext = null;

                foreach (IDisposable disposable in FEntries.Values)
                {
                    disposable.Dispose();
                }

                FEntries.Clear();
                FEntries = null;
            }
        }

        protected override IInjector Self => (IInjector) Get(typeof(IInjector));
        #endregion

        /// <summary>
        /// Creates a new <see cref="IInjector"/> instance.
        /// </summary>
        /// <returns>The newly created instance.</returns>
        public static IInjector Create() => new Injector().Self;
    }
}
