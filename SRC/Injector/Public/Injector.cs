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
        private readonly ConcurrentDictionary<Type, InjectorEntry> mEntries = new ConcurrentDictionary<Type, InjectorEntry>();

        private readonly ThreadLocal<ThreadContext> mContext = new ThreadLocal<ThreadContext>(() => new ThreadContext
        {
            CurrentPath = new Type[0]
        }, trackAllValues: false);

        public Injector()
        {    
            mEntries.GetOrAdd(typeof(IInjector), new InjectorEntry
            {
                Interface = typeof(IInjector),
                Type      = DependencyType.__Self,
                Factory   = () => this
            });
        }

        #region Helpers
        private ThreadContext Context => mContext.Value;

        private InjectorEntry Service(Type iface, Type implementation, DependencyType type)
        {
            Check.NotNull(iface, nameof(iface));
            Check.NotNull(implementation, nameof(implementation));

            if (type > DependencyType.Singleton)
                throw new ArgumentException(Resources.INVALID_DEPENDENCY_TYPE, nameof(type));

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
                throw new NotSupportedException(
                    string.Format(Resources.CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED, implementation));

            var entry = new InjectorEntry
            {
                Interface      = iface,
                Implementation = implementation,
                Type           = type
            };

            //
            // Ha generikus interface-t regisztralunk akkor nem kell (nem is lehet) 
            // legyartani a factory-t.
            //

            if (!iface.IsGenericTypeDefinition) entry.Factory = CreateFactory(constructors[0]); // idoigenyes

            //
            // Bejegyzes felvetele (szal biztos -> lehet egy korabbi hivas bejegyzeset adja vissza
            // de azzal sincs gond).
            //

            return mEntries.GetOrAdd(iface, entry);
        }

        private InjectorEntry Factory(Type iface, Func<IInjector, Type, object> factory, DependencyType type)
        {
            Check.NotNull(iface,   nameof(iface));
            Check.NotNull(factory, nameof(factory));

            if (type > DependencyType.Singleton)
                throw new ArgumentException(Resources.INVALID_DEPENDENCY_TYPE, nameof(type));

            if (!iface.IsInterface)
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(iface));

            return mEntries.GetOrAdd(iface, new InjectorEntry
            {
                Factory = () =>
                {
                    object instance = factory(this, iface);
                    if (!iface.IsInstanceOfType(instance))
                        throw new Exception(string.Format(Resources.INVALID_TYPE, iface));

                    return instance;
                },
                Interface = iface,
                Type = type
            });
        }

        private bool GetEntry(Type iface, out InjectorEntry entry)
        {
            return mEntries.TryGetValue(iface, out entry);
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
                        Expression.Call(Expression.Constant(this), ((Func<Type, object>) (Get)).Method, Expression.Constant(para.ParameterType)), 
                        para.ParameterType)
                    )
                )
            ).Compile();     
        }

        private object Get(Type iface)
        {
            Check.NotNull(iface, nameof(iface));

            IReadOnlyList<Type> oldPath = Context.CurrentPath;
            try
            {
                //
                // Miutan az aktualis utvonalat bovitettuk sajat magunkkal ellenorizzuk 
                // h nincs e korkoros referencia.
                //

                IReadOnlyList<Type> currentPath = Context.CurrentPath = new List<Type>(oldPath) {iface};
                if (currentPath.Count(t => t == iface) > 1)
                    throw new InvalidOperationException(string.Format(Resources.CIRCULAR_REFERENCE, string.Join(" -> ", currentPath)));

                //
                // Generikus tipusokat nem lehet peldanyositani.
                //

                if (iface.IsGenericTypeDefinition)
                    throw new InvalidOperationException(Resources.CANT_INSTANTIATE);

                InjectorEntry entry;
                if (!GetEntry(iface, out entry))
                {
                    //
                    // Meg benne lehet generikus formaban.
                    //

                    if (!iface.IsGenericType || !GetEntry(iface.GetGenericTypeDefinition(), out entry))
                        throw new NotSupportedException(string.Format(Resources.DEPENDENCY_NOT_FOUND, iface));

                    //
                    // Ha a bejegyzesnek van kezzel felvett [Factory()] factory fv-e (akar generikusnak is)
                    // akkor nincs dolgunk.
                    //

                    if (entry.Factory == null)
                        //
                        // Regisztraljuk az uj konkret tipust. Nem gond ha parhuzamosan ide tobb szal
                        // is eljut, mert a regisztracio ugy is csak egyszer kerul be a rendszerbe.
                        //

                        entry = Service(iface, entry.Implementation.MakeGenericType(iface.GetGenericArguments()), entry.Type);
                }
#if DEBUG
                Debug.Assert(entry.Factory != null);
#endif
                if (entry.Type == DependencyType.Singleton)
                {
                    lock (entry)
                    {
                        if (entry.Type == DependencyType.Singleton)
                        {
                            object instance = entry.Factory();

                            entry.Factory = () => instance;
                            entry.Type = DependencyType.__InstantiatedSingleton;
                        }
                    }
                }

                return entry.Factory();
            }
            finally
            {
                Context.CurrentPath = oldPath;
            }
        }

        private static bool IsAssignableFrom(Type iface, Type implementation)
        {
            //
            // Az IsAssignableFrom() csak nem generikus tipusokra mukodik (nem szamit
            // h a tipus mar tipizalva lett e v sem).
            //

            if (iface.IsAssignableFrom(implementation)) return true;

            //
            // Innentol csak akkor kell tovabb mennunk ha mindket tipusunk generikus.
            //

            if (!iface.IsGenericType || !implementation.IsGenericType) return false;

            //
            // "List<> -> IList<>"
            //

            if (iface.IsGenericTypeDefinition && implementation.IsGenericTypeDefinition)
            {
                return implementation.GetInterfaces().Where(i => i.IsGenericType).Any(i => i.GetGenericTypeDefinition() == iface);
            }

            //
            // "List<T> -> IList<T>"
            //

            if (!iface.IsGenericTypeDefinition && !implementation.IsGenericTypeDefinition)
            {
                return 
                    iface.GetGenericArguments().SequenceEqual(implementation.GetGenericArguments()) &&
                    IsAssignableFrom(iface.GetGenericTypeDefinition(), implementation.GetGenericTypeDefinition());
            }

            //
            // "List<T> -> IList<>", "List<> -> IList<T>"
            //

            return false;
        }
        #endregion

        #region IInjector            
        IInjector IInjector.Service(Type iface, Type implementation, DependencyType type)
        {
            Service(iface, implementation, type); // ellenorzi a parametereket
            return this;
        }

        IInjector IInjector.Service<TInterface, TImplementation>(DependencyType type)
        {
            Service(typeof(TInterface), typeof(TImplementation), type);
            return this;
        }

        IInjector IInjector.Factory(Type iface, Func<IInjector, Type, object> factory, DependencyType type)
        {
            Factory(iface, factory, type);
            return this;
        }

        IInjector IInjector.Factory<TInterface>(Func<IInjector, TInterface> factory, DependencyType type)
        {
            Factory(typeof(TInterface), (me, @void) => factory(me), type);
            return this;
        }

        IInjector IInjector.Proxy(Type iface, Func<IInjector, Type, object, object> factory)
        {
            throw new NotImplementedException();
        }

        IInjector IInjector.Proxy<TInterface>(Func<IInjector, TInterface, TInterface> factory)
        {
            throw new NotImplementedException();
        }

        object IInjector.Get(Type iface)
        {
            return Get(iface); // ellenorzi a parametert
        }

        TInterface IInjector.Get<TInterface>()
        {
            return (TInterface) Get(typeof(TInterface));
        }
        #endregion

        void IDisposable.Dispose()
        {
            foreach (IDisposable disposable in mEntries
                .Values
                .Where(entry => entry.Type == DependencyType.__InstantiatedSingleton)
                .Select(entry => entry.Factory() as IDisposable)
                .Where(value => value != null))
            {
                    disposable.Dispose(); 
            }

            mEntries.Clear(); 
        }
    }
}
