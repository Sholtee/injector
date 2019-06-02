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
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace Solti.Utils.DI
{
    using Properties;
    using Internals;

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

            IReadOnlyDictionary<Type, InjectorEntry> entriesToCopy = new Dictionary<Type, InjectorEntry>(0);

            if (parent != null) entriesToCopy = parent
                .FEntries
                .Values
                .Where(entry => entry.Interface != typeof(IInjector))
                .ToDictionary(
                    entry => entry.Interface,
                    entry => (InjectorEntry) entry.Clone());

            FEntries = new ConcurrentDictionary<Type, InjectorEntry>(entriesToCopy);

            FContext = new ThreadLocal<ThreadContext>(() => new ThreadContext(), trackAllValues: false);

            //
            // Felvesszuk sajat magunkat.
            //

            Instance(typeof(IInjector), new ParameterValidatorProxy<IInjector>(this).Proxy, releaseOnDispose: false);
        }

        private static readonly MethodInfo IfaceGet = ((MethodCallExpression) ((Expression<Action<IInjector>>) (injector => injector.Get(null))).Body).Method;

        private Func<IInjector, Type, object> CreateFactory(ConstructorInfo constructor)
        {
            //
            // (injector, type) => (IService) new Service((IDependency_1) injector.Get(typeof(IDependency_1)), ...)
            //

            ParameterExpression injector = Expression.Parameter(typeof(IInjector), "injector");

            return Expression.Lambda<Func<IInjector, Type, object>>
            (
                Expression.New
                (
                    constructor,
                    constructor.GetParameters().Select(para => Expression.Convert
                    (
                        Expression.Call(injector, IfaceGet, Expression.Constant(para.ParameterType)),
                        para.ParameterType
                    ))
                ),
                injector,

                //
                // Csak azert kell h a legyartott factory layout-ja stimmeljen.
                //

                Expression.Parameter(typeof(Type), "type")
            ).Compile();
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
                throw new Exception(string.Format(Resources.INVALID_TYPE, type));

            return inst;
        }

        private static ConstructorInfo ValidateImplementation(Type iface, Type implementation)
        {
            if (!iface.IsInterfaceOf(implementation))
                throw new InvalidOperationException(string.Format(Resources.NOT_ASSIGNABLE, iface, implementation));

            //
            // Az implementacionak pontosan egy konstruktoranak kell lennie.
            //

            IReadOnlyList<ConstructorInfo> constructors = implementation.GetConstructors();
            if (constructors.Count > 1)
                throw new NotSupportedException(string.Format(Resources.CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED, implementation));

            return constructors[0];
        }
        #endregion

        #region Internal
        internal InjectorEntry Service(Type iface, Type implementation, Lifetime? lifetime)
        {
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

                Factory = !iface.IsGenericTypeDefinition ? CreateFactory(constructor) : null
            });
        }

        internal InjectorEntry Factory(Type iface, Func<IInjector, Type, object> factory, Lifetime? lifetime)
        {
            return Register(new InjectorEntry(iface, lifetime: lifetime)
            {
                Factory = factory
            });
        }

        internal InjectorEntry Lazy(Type iface, IResolver resolver, Lifetime? lifetime)
        {
            Func<IInjector, Type, object> realFactory = null;

            var entry = new InjectorEntry(iface, typeof(IResolver), lifetime);
            entry.Factory = (injector, type) =>
            {
                //
                // A varazslat az hogy a "realFactory" es "entry" meg itt is letezik hiaba 
                // kerult a Factory kesobb meghivasra.
                //

                if (realFactory == null)
                    lock (entry)
                        if (realFactory == null)
                            realFactory = CreateFactory(ValidateImplementation(iface, resolver.Resolve(iface)));

                return realFactory(injector, type);
            };

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
            // Kulomben vegyuk fel az uj tipizalt bejegyzest. Megjegyzendo h a MakeGenericType() es Service()
            // hivas idoigenyes.
            //

            lock (genericEntry)
                return GetEntry(iface, out entry) 
                    ? entry 
                    : Service(iface, genericEntry.Implementation.MakeGenericType(iface.GetGenericArguments()), genericEntry.Lifetime);           
        }

        internal InjectorEntry Proxy(Type iface, Func<IInjector, Type, object, object> decorator)
        {
            //
            // Instance() hivassal felvett ertek vagy generikus szerviz eseten a metodus nem ertelmezett.
            //

            InjectorEntry entry = GetEntry(iface);

            if (entry.Factory == null)
                throw new InvalidOperationException(Resources.CANT_PROXY);

            //
            // Lock igazabol nem is kene, csak kis paranoia.
            //

            lock (entry)
            {
                Func<IInjector, Type, object> oldFactory = entry.Factory;

                entry.Factory = (injector, type) => decorator(injector, type, oldFactory(injector, type));
            }

            return entry;
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
        IInjector IInjector.Service(Type iface, Type implementation, Lifetime lifetime)
        {
            Service(iface, implementation, lifetime);
            return Self;
        }

        IInjector IInjector.Lazy(Type iface, IResolver resolver, Lifetime lifetime)
        {
            Lazy(iface, resolver, lifetime);
            return Self;
        }

        IInjector IInjector.Factory(Type iface, Func<IInjector, Type, object> factory, Lifetime lifetime)
        {
            Factory(iface, factory, lifetime);

            //
            // A visszaadott peldany tipusat meg ellenorizni kell.
            //

            Proxy(iface, TypeChecker);

            return Self;
        }

        IInjector IInjector.Instance(Type iface, object instance, bool releaseOnDispose)
        {
            Instance(iface, instance, releaseOnDispose);
            return Self;
        }

        IInjector IInjector.Proxy(Type iface, Func<IInjector, Type, object, object> decorator)
        {
            Proxy(iface, decorator);

            //
            // A visszaadott peldany tipusat meg ellenorizni kell.
            //

            Proxy(iface, TypeChecker);

            return Self;
        }

        object IInjector.Get(Type iface) => Get(iface);

        IReadOnlyList<Type> IInjector.Entries => FEntries.Keys.ToArray();

        IServiceInfo IInjector.QueryServiceInfo(Type iface) => GetEntry(iface);
        #endregion

        #region ILockable
        bool ILockable.Locked => FLocked;

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

        /// <summary>
        /// "this" helyett hasznalando, hogy mindig a proxy-zott peldanyt adjuk vissza.
        /// </summary>
        protected override IInjector Self => (IInjector) Get(typeof(IInjector));
        #endregion

        /// <summary>
        /// Creates a new <see cref="IInjector"/> instance.
        /// </summary>
        /// <returns>The newly created instance.</returns>
        public static IInjector Create() => new Injector().Self;
    }
}
