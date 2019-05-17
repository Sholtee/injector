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

    public sealed class Injector : IInjector
    {
        #region Private
        private readonly ConcurrentDictionary<Type, InjectorEntry> FEntries;

        private readonly ThreadLocal<ThreadContext> FContext;

        private ThreadContext Context => FContext.Value;

        private IInjector Self => (IInjector) Get(typeof(IInjector));

        private Injector(): this(new InjectorEntry[0])
        {
        }

        private Injector(IEnumerable<InjectorEntry> entriesToCopy)
        {
            FEntries = new ConcurrentDictionary<Type, InjectorEntry>(entriesToCopy.ToDictionary(
                entry => entry.Interface, 
                entry => (InjectorEntry) entry.Clone()));

            FContext = new ThreadLocal<ThreadContext>(() => new ThreadContext
            {
                CurrentPath = new Type[0]
            }, trackAllValues: false);

            //
            // Felvesszuk sajat megunkat. Megjegyzendo h mivel peldanykent vesszuk fel 
            // magunkat ezert kell a "kezi" proxy-zas, egyebkent Proxy() hivast kene 
            // hasznalni.
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
        #endregion

        #region Internal
        internal InjectorEntry Service(Type iface, Type implementation, Lifetime? lifetime)
        {
            if (!iface.IsInterface)
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(iface));

            if (!implementation.IsClass)
                throw new ArgumentException(Resources.NOT_A_CLASS, nameof(implementation));

            if (!IsAssignableFrom(iface, implementation))
                throw new InvalidOperationException(string.Format(Resources.NOT_ASSIGNABLE, iface, implementation));

            //
            // Az implementacionak pontosan egy konstruktoranak kell lennie.
            //

            IReadOnlyList<ConstructorInfo> constructors = implementation.GetConstructors();
            if (constructors.Count > 1)
                throw new NotSupportedException(string.Format(Resources.CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED, implementation));

            //
            // Bejegyzes felvetele (szal biztos -> lehet egy korabbi hivas bejegyzeset adja vissza
            // de azzal sincs gond).
            //

            return FEntries.GetOrAdd(iface, new InjectorEntry
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
            if (!iface.IsInterface)
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(iface));

            return FEntries.GetOrAdd(iface, new InjectorEntry
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
            //
            // Regisztraljuk az uj konkret tipust. Nem gond ha parhuzamosan ide tobb szal
            // is eljut, mert a regisztracio ugy is csak egyszer kerul be a rendszerbe.
            //

            return Service(iface, entry.Implementation.MakeGenericType(iface.GetGenericArguments()), entry.Lifetime);
        }

        internal InjectorEntry Proxy(Type iface, Func<Type, object, object> decorator)
        {
            if (!iface.IsInterface)
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(iface));

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
            if (!iface.IsInterface)
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(iface));

            Type instanceType = instance.GetType();

            if (!IsAssignableFrom(iface, instanceType))
                throw new InvalidOperationException(string.Format(Resources.NOT_ASSIGNABLE, iface, instanceType));

            return FEntries.GetOrAdd(iface, new InjectorEntry
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
                // Generikus tipusokat nem lehet peldanyositani.
                //

                if (iface.IsGenericTypeDefinition)
                    throw new InvalidOperationException(Resources.CANT_INSTANTIATE_GENERICS);

                InjectorEntry entry = GetEntry(iface);

                //
                // Ha singleton eletciklusunk van akkor ha meg eddig nem volt akkor le kell 
                // gyartanunk a peldanyt.
                //

                if (entry.Lifetime == Lifetime.Singleton && entry.Value == null)
                    lock (entry)
                        if (entry.Value == null)
                            return entry.Value = entry.Factory(iface);

                //
                // Elvileg jok vagyunk: Ha van "Value"-nk ("Singleton") akkor visszaadjuk azt, 
                // kulomben legyartjuk az uj peldanyt.
                //
                // Megjegyzes: 
                //   Ne az "entry.Interface"-t adjuk at parameterkent mert az lehet h generikus.
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
        //
        // Ebben a regioban CSAK explicit interface implementaciok legyenek, hogy a 
        // hook-ok mindig meghivasra keruljenek.
        //

        IInjector IInjector.Service(Type iface, Type implementation, Lifetime lifetime)
        {
            Service(iface, implementation, lifetime);
            return Self;
        }

        IInjector IInjector.Service<TInterface, TImplementation>(Lifetime lifetime)
        {
            Service(typeof(TInterface), typeof(TImplementation), lifetime);
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

        IInjector IInjector.Factory<TInterface>(Func<IInjector, TInterface> factory, Lifetime lifetime)
        {
            Factory(typeof(TInterface), type => factory(Self), lifetime);
            return Self;
        }

        IInjector IInjector.Instance(Type iface, object instance)
        {
            Instance(iface, instance);
            return Self;
        }

        IInjector IInjector.Instance<TInterface>(TInterface instance)
        {
            Instance(typeof(TInterface), instance);
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

        IInjector IInjector.Proxy<TInterface>(Func<IInjector, TInterface, TInterface> decorator)
        {
            Proxy(typeof(TInterface), (type, current) => decorator(Self, (TInterface) current));
            return Self;
        }

        object IInjector.Get(Type iface)
        {
            return Get(iface);
        }

        TInterface IInjector.Get<TInterface>()
        {
            return (TInterface) Get(typeof(TInterface));
        }

        IInjector IInjector.CreateChild()
        {
            //
            // A sajat magunkat tartalmazo bejegyzesen kivul az osszes tobbi bejegyzes
            // masolasa (szal biztos). A masolas mikentjet lasd az InjectorEntry 
            // implementaciojaban.
            //

            return new Injector(FEntries.Values.Where(entry => entry.Interface != typeof(IInjector))).Self;
        }
        #endregion

        #region IDisposable
        void IDisposable.Dispose()
        {
            foreach (IDisposable disposable in FEntries
                .Values
                .Where(entry => entry.Lifetime == Lifetime.Singleton)
                .Select(entry => entry.Value as IDisposable)
                .Where(value => value != null))
            {
                    disposable.Dispose(); 
            }

            FEntries.Clear(); 
        }
        #endregion

        public static IInjector Create()
        {
            return new Injector().Self;
        }
    }
}
