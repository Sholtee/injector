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
        private readonly ConcurrentDictionary<Type, InjectorEntry> FEntries = new ConcurrentDictionary<Type, InjectorEntry>();

        private readonly ThreadLocal<ThreadContext> FContext = new ThreadLocal<ThreadContext>(() => new ThreadContext
        {
            CurrentPath = new Type[0]
        }, trackAllValues: false);

        private ThreadContext Context => FContext.Value;

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

        private Func<object> CreateFactory(ConstructorInfo constructor)
        {
            return Expression.Lambda<Func<object>>
            (
                Expression.New
                (
                    constructor,
                    constructor.GetParameters().Select(para => Expression.Convert
                        (
                            Expression.Call(Expression.Constant(this), ((Func<Type, object>)(Get)).Method, Expression.Constant(para.ParameterType)),
                            para.ParameterType)
                    )
                )
            ).Compile();
        }

        private bool GetEntry(Type iface, out InjectorEntry entry)
        {
            return FEntries.TryGetValue(iface, out entry);
        }

        private Func<object> ConvertToTypeChecked(Func<object> factory, Type expectedType)
        {
            return () =>
            {
                object instance = factory();

                //
                // A letrhozott peldany tipusat ellenorizzuk.
                //

                if (!expectedType.IsInstanceOfType(instance))
                    throw new Exception(string.Format(Resources.INVALID_TYPE, expectedType));

                return instance;
            };
        }
        #endregion

        #region Internal
        internal InjectorEntry Service(Type iface, Type implementation, Lifetime lifetime)
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

        internal InjectorEntry Factory(Type iface, Func<object> factory, Lifetime lifetime)
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
            Debug.Assert(entry.Implementation.IsGenericTypeDefinition);
#endif
            //
            // Regisztraljuk az uj konkret tipust. Nem gond ha parhuzamosan ide tobb szal
            // is eljut, mert a regisztracio ugy is csak egyszer kerul be a rendszerbe.
            //

            return Service(iface, entry.Implementation.MakeGenericType(iface.GetGenericArguments()), entry.Lifetime);
        }

        internal InjectorEntry Proxy(Type iface, Func<object, object> decorator)
        {
            if (!iface.IsInterface)
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(iface));

            //
            // Csak konkret tipusokat proxy-zhatunk.
            //

            if (iface.IsGenericTypeDefinition)
                throw new InvalidOperationException(Resources.CANT_PROXY_GENERICS);

            InjectorEntry entry = GetEntry(iface);

            lock (entry) // igazabol ez nem is kene, de biztos ami tuti
            {
                Func<object> oldFactory = entry.Factory;

                entry.Factory = () => decorator(oldFactory());
            }

            return entry;
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
#if DEBUG
                Debug.Assert(entry.Factory != null);
#endif
                //
                // Ha singleton eletciklusunk van akkor ha meg eddig nem volt akkor le kell 
                // gyartanunk a peldanyt.
                //

                if (entry.Lifetime == Lifetime.Singleton && entry.Value == null)
                    lock (entry)
                        if (entry.Value == null)
                            return entry.Value = entry.Factory();

                //
                // Elvileg jok vagyunk: Ha van "Value"-nk ("Singleton") akkor visszaadjuk azt, 
                // kulomben legyartjuk az uj peldanyt.
                //

                return entry.Value ?? entry.Factory();
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
            Check.NotNull(iface,          nameof(iface));
            Check.NotNull(implementation, nameof(implementation));

            Service(iface, implementation, lifetime);
            return this;
        }

        IInjector IInjector.Service<TInterface, TImplementation>(Lifetime lifetime)
        {
            Service(typeof(TInterface), typeof(TImplementation), lifetime);
            return this;
        }

        IInjector IInjector.Factory(Type iface, Func<IInjector, Type, object> factory, Lifetime lifetime)
        {
            Check.NotNull(iface,   nameof(iface));
            Check.NotNull(factory, nameof(factory));

            Factory(iface, ConvertToTypeChecked(() => factory(this, iface), iface), lifetime);
            return this;
        }

        IInjector IInjector.Factory<TInterface>(Func<IInjector, TInterface> factory, Lifetime lifetime)
        {
            Check.NotNull(factory, nameof(factory));

            Factory(typeof(TInterface), () => factory(this), lifetime);
            return this;
        }

        IInjector IInjector.Proxy(Type iface, Func<IInjector, Type, object, object> decorator)
        {
            Check.NotNull(iface,   nameof(iface));
            Check.NotNull(decorator, nameof(decorator));

            throw new NotImplementedException();
        }

        IInjector IInjector.Proxy<TInterface>(Func<IInjector, TInterface, TInterface> decorator)
        {
            Check.NotNull(decorator, nameof(decorator));

            Proxy(typeof(TInterface), current => decorator(this, (TInterface) current));
            return this;
        }

        object IInjector.Get(Type iface)
        {
            Check.NotNull(iface, nameof(iface));

            return Get(iface);
        }

        TInterface IInjector.Get<TInterface>()
        {
            return (TInterface) Get(typeof(TInterface));
        }
        #endregion

        #region IDisposable
        void IDisposable.Dispose()
        {
            foreach (IDisposable disposable in FEntries
                .Values
                .Where(entry => entry.Lifetime == Lifetime.Singleton)
                .Select(entry => entry.Value as IDisposable)
                .Where(value => value != null && !(value is IInjector)))
            {
                    disposable.Dispose(); 
            }

            FEntries.Clear(); 
        }
        #endregion

        public Injector()
        {
            Factory(typeof(IInjector), () => this, Lifetime.Singleton);
        }
    }
}
