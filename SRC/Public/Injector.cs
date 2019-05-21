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

    public sealed class Injector : Composite<IInjector>, IInjector
    {
        #region Private
        private /*readonly*/ IDictionary<Type, InjectorEntry> FEntries;

        private /*readonly*/ ThreadLocal<ThreadContext> FContext;

        /// <summary>
        /// Az aktualis szal kontextusa.
        /// </summary>
        private ThreadContext Context => FContext.Value;

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

            FContext = new ThreadLocal<ThreadContext>(() => new ThreadContext
            {
                CurrentPath = new Type[0]
            }, trackAllValues: false);

            //
            // Felvesszuk sajat magunkat.
            //

            Instance(typeof(IInjector), new ParameterValidator<IInjector>(this).Proxy);
        }

        private static bool IsAssignableFrom(Type iface, Type implementation)
        {
            //
            // Az IsAssignableFrom() csak nem generikus tipusokra mukodik (nem szamit
            // h a tipus mar tipizalva lett e v sem).
            //

            if (iface.IsAssignableFrom(implementation))
                return true;

            //
            // Innentol csak akkor kell tovabb mennunk ha mindket tipusunk generikus.
            //

            if (!iface.IsGenericType || !implementation.IsGenericType)
                return false;

            //
            // "List<> -> IList<>"
            //

            if (iface.IsGenericTypeDefinition && implementation.IsGenericTypeDefinition)
                return implementation.GetInterfaces().Where(i => i.IsGenericType).Any(i => i.GetGenericTypeDefinition() == iface);

            //
            // "List<T> -> IList<T>"
            //

            if (!iface.IsGenericTypeDefinition && !implementation.IsGenericTypeDefinition)
                return
                    iface.GetGenericArguments().SequenceEqual(implementation.GetGenericArguments()) &&
                    IsAssignableFrom(iface.GetGenericTypeDefinition(), implementation.GetGenericTypeDefinition());

            //
            // "List<T> -> IList<>", "List<> -> IList<T>"
            //

            return false;
        }

        private Func<Type, object> CreateFactory(ConstructorInfo constructor)
        {
            //
            // type => (IService) new Service((IDependency_1) Get("dependency_1"), (IDependency_2) Get("dependency_2"), ...)
            //

            return Expression.Lambda<Func<Type, object>>
            (
                Expression.New
                (
                    constructor,
                    constructor.GetParameters().Select(para => Expression.Convert
                    (
                        Expression.Call(Expression.Constant(this), ((Func<Type, object>) (Get)).Method, Expression.Constant(para.ParameterType)),
                        para.ParameterType
                    ))
                ),

                //
                // Csak azert kell h a legyartott factory layout-ja stimmeljen.
                //

                Expression.Parameter(typeof(Type), "type")
            ).Compile();
        }

        private bool GetEntry(Type iface, out InjectorEntry entry)
        {
            return FEntries.TryGetValue(iface, out entry);
        }

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
        #endregion

        #region Internal
        internal InjectorEntry Service(Type iface, Type implementation, Lifetime? lifetime)
        {
            if (!IsAssignableFrom(iface, implementation))
                throw new InvalidOperationException(string.Format(Resources.NOT_ASSIGNABLE, iface, implementation));

            //
            // Az implementacionak pontosan egy konstruktoranak kell lennie.
            //

            IReadOnlyList<ConstructorInfo> constructors = implementation.GetConstructors();
            if (constructors.Count > 1)
                throw new NotSupportedException(string.Format(Resources.CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED, implementation));

            //
            // Bejegyzes felvetele.
            //

            return Register(new InjectorEntry
            {
                Interface      = iface,
                Implementation = implementation,
                Lifetime       = lifetime,

                //
                // Ha generikus interface-t regisztralunk akkor nem kell (nem is lehet) 
                // legyartani a factory-t.
                //

                Factory = !iface.IsGenericTypeDefinition ? CreateFactory(constructors[0]) : null
            });
        }

        internal InjectorEntry Factory(Type iface, Func<Type, object> factory, Lifetime? lifetime)
        {
            return Register(new InjectorEntry
            {
                Factory   = factory,
                Interface = iface,
                Lifetime  = lifetime
            });
        }

        internal InjectorEntry GetEntry(Type iface)
        {
            InjectorEntry entry;
            if (GetEntry(iface, out entry)) return entry;
     
            //
            // Meg benne lehet generikus formaban.
            //

            if (!iface.IsGenericType || !GetEntry(iface.GetGenericTypeDefinition(), out entry))
                throw new NotSupportedException(string.Format(Resources.DEPENDENCY_NOT_FOUND, iface));

            //
            // Ha a bejegyzesnek van kezzel felvett [Factory()] factory fv-e (akar generikusnak is)
            // akkor nincs dolgunk.
            //

            if (entry.Factory != null) return entry;
#if DEBUG
            Debug.Assert(entry.Implementation != null && entry.Implementation.IsGenericTypeDefinition, "Not a generic type definition");
            Debug.Assert(entry.Lifetime != null, "Lifetime is NULL");
#endif
            try
            {
                //
                // Regisztraljuk az uj konkret tipust.
                //

                return Service(iface, entry.Implementation.MakeGenericType(iface.GetGenericArguments()), entry.Lifetime);
            }
            catch (ServiceAlreadyRegisteredException)
            {
                //
                // Felteve ha nem regisztralta ido kozben mas.
                //

                return GetEntry(iface);
            }            
        }

        internal InjectorEntry Proxy(Type iface, Func<Type, object, object> decorator)
        {
            //
            // Instance() hivassal felvett ertek vagy generikus szerviz eseten a metodus nem ertelmezett.
            //

            InjectorEntry entry = GetEntry(iface);
            if (entry.Factory == null)
                throw new InvalidOperationException(Resources.CANT_PROXY);

            lock (entry) // igazabol ez nem is kene, de biztos ami tuti
            {
                Func<Type, object> oldFactory = entry.Factory;

                entry.Factory = type => decorator(type, oldFactory(type));
            }

            return entry;
        }

        internal InjectorEntry Instance(Type iface, object instance)
        {
            Type instanceType = instance.GetType();

            if (!IsAssignableFrom(iface, instanceType))
                throw new InvalidOperationException(string.Format(Resources.NOT_ASSIGNABLE, iface, instanceType));

            return Register(new InjectorEntry
            {
                Interface = iface,
                Value     = instance
            });
        }

        internal object Get(Type iface)
        {
            IReadOnlyList<Type> oldPath = Context.CurrentPath;
            try
            {
                //
                // Miutan az utvonalat bovitettuk az aktualis interface-el a tipus csak 
                // egyszer szerpelhet a listaban. Kulomben korkoros referencia.
                //

                IReadOnlyList<Type> currentPath = Context.CurrentPath = new List<Type>(oldPath) {iface};
                if (currentPath.Count(t => t == iface) > 1)
                    throw new InvalidOperationException(string.Format(Resources.CIRCULAR_REFERENCE, string.Join(" -> ", currentPath)));

                //
                // Ha singleton eletciklusunk van akkor ha meg eddig nem volt akkor le kell 
                // gyartanunk a peldanyt.
                //

                InjectorEntry entry = GetEntry(iface);

                if (entry.Lifetime == Lifetime.Singleton && entry.Value == null)
                    lock (entry)
                        if (entry.Value == null)
                            return entry.Value = entry.Factory(iface);

                //
                // Elvileg jok vagyunk: Ha van "Value"-nk ("Singleton" eletciklus vagy Instance() hivas) 
                // akkor visszaadjuk azt, kulomben legyartjuk az uj peldanyt.
                //

                return entry.Value ?? entry.Factory(iface);
            }
            finally
            {
                Context.CurrentPath = oldPath;
            }
        }
        #endregion

        #region IInjector   
        IInjector IInjector.Service(Type iface, Type implementation, Lifetime lifetime)
        {
            Service(iface, implementation, lifetime);
            return Self;
        }

        IInjector IInjector.Factory(Type iface, Func<IInjector, Type, object> factory, Lifetime lifetime)
        {
            object TypeChecked(Type type)
            {
                object instance = factory(Self, type);

                //
                // A letrhozott peldany tipusat ellenorizzuk.
                //

                if (!type.IsInstanceOfType(instance))
                    throw new Exception(string.Format(Resources.INVALID_TYPE, type));

                return instance;
            }

            Factory(iface, TypeChecked, lifetime);
            return Self;
        }

        IInjector IInjector.Instance(Type iface, object instance)
        {
            Instance(iface, instance);
            return Self;
        }

        IInjector IInjector.Proxy(Type iface, Func<IInjector, Type, object, object> decorator)
        {
            object TypeChecked(Type type, object inst)
            {
                inst = decorator(Self, type, inst);

                //
                // A letrhozott peldany tipusat ellenorizzuk. 
                //

                if (!type.IsInstanceOfType(inst))
                    throw new Exception(string.Format(Resources.INVALID_TYPE, type));

                return inst;
            }

            Proxy(iface, TypeChecked);
            return Self;
        }

        object IInjector.Get(Type iface)
        {
            return Get(iface);
        }

        IReadOnlyList<Type> IInjector.Entries => FEntries.Keys.ToArray();
        #endregion

        #region Protected
        protected override IInjector CreateChild()
        {
            return new Injector(this).Self;
        }

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

        public static IInjector Create()
        {
            return new Injector().Self;
        }
    }
}
