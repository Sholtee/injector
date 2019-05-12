/********************************************************************************
* Injector.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.Injector
{
    using Properties;

    public sealed class Injector : IInjector
    {
        private readonly ConcurrentDictionary<Type, InjectorEntry> mEntries = new ConcurrentDictionary<Type, InjectorEntry>();

        public Injector()
        {
            mEntries.GetOrAdd(typeof(IInjector), new InjectorEntry
            {
                Interface = typeof(IInjector),
                Type      = DependencyType.Self,
                Factory   = @void => this
            });
        }

        #region Helpers
        private InjectorEntry Register(Type iface, Type implementation, DependencyType type)
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
                throw new NotSupportedException(string.Format(Resources.CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED, implementation));

            //
            // Bejegyzes felvetele (szal biztos).
            //

            return mEntries.GetOrAdd(iface, @void => iface.IsGenericTypeDefinition
                //
                // Ha generikus interface-t regisztralunk akkor nem kell (nem is lehet) 
                // legyartani a factory-t.
                //

                ? new InjectorEntry
                {
                    Interface             = iface,
                    GenericImplementation = implementation,
                    Type                  = type
                }
                : new InjectorEntry
                {
                    Interface = iface,
                    Factory   = CreateFactory(constructors[0]), // idoigenyes
                    Type      = type
                });
        }

        private bool GetEntry(Type iface, out InjectorEntry entry)
        {
            return mEntries.TryGetValue(iface, out entry);
        }

        private Func<IReadOnlyList<Type>, object> CreateFactory(ConstructorInfo constructor)
        { 
            ParameterExpression currentPath = Expression.Parameter(typeof(IReadOnlyList<Type>), "currentPath");

            return Expression.Lambda<Func<IReadOnlyList<Type>, object>>
            (
                Expression.New
                (
                    constructor,
                    constructor.GetParameters().Select(para => Expression.Convert
                    (
                        Expression.Call(Expression.Constant(this), ((Func<Type, IReadOnlyList<Type>, object>) (Get)).Method, Expression.Constant(para.ParameterType), currentPath), 
                        para.ParameterType)
                    )
                ),
                currentPath
            ).Compile();     
        }

        private object Get(Type iface, IReadOnlyList<Type> currentPath)
        {
            //
            // Miutana az aktualis utvonalat bovitettuk sajat magunkkal ellenorizzuk 
            // h nincs e korkoros referencia.
            //

            currentPath = new List<Type>(currentPath) {iface};
            if (currentPath.Count(t => t == iface) > 1)
                throw new InvalidOperationException(string.Format(Resources.CIRCULAR_REFERENCE, string.Join(" -> ", currentPath)));

            InjectorEntry entry;
            if (!GetEntry(iface, out entry))
            {
                //
                // Meg benne lehet generikus formaban.
                //

                if (!iface.IsGenericType || !GetEntry(iface.GetGenericTypeDefinition(), out entry))
                    throw new NotSupportedException(string.Format(Resources.DEPENDENCY_NOT_FOUND, iface));

                //
                // Regisztraljuk az uj konkret tipust. Nem gond ha parhuzamosan ide tobb szal
                // is eljut, mert a regisztracio ugy is csak egyszer kerul be a rendszerbe.
                //

                entry = Register(iface, entry.GenericImplementation.MakeGenericType(iface.GetGenericArguments()), entry.Type);
            }

            if (entry.GenericImplementation != null)
                throw new InvalidOperationException(Resources.CANT_INSTANTIATE);

            if (entry.Type == DependencyType.Singleton)
            {
                lock (entry)
                {
                    if (entry.Type == DependencyType.Singleton)
                    {
                        object instance = entry.Factory(currentPath);

                        entry.Factory = @void => instance;
                        entry.Type = DependencyType.InstantiatedSingleton;
                    }
                }
            }

            return entry.Factory(currentPath);
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

        IDecorator IInjector.Register(Type iface, Type implementation, DependencyType type)
        {
            return new Decorator(Register(iface, implementation, type));
        }

        IDecorator<TInterface> IInjector.Register<TInterface, TImplementation>(DependencyType type)
        {
            return new Decorator<TInterface>(Register(typeof(TInterface), typeof(TImplementation), type));
        }

        object IInjector.Get(Type iface)
        {
            Check.NotNull(iface, nameof(iface));

            return Get(iface, new Type[0]);
        }

        TInterface IInjector.Get<TInterface>()
        {
            return (TInterface) Get(typeof(TInterface), new Type[0]);
        }

        void IDisposable.Dispose()
        {
            foreach (IDisposable disposable in mEntries
                .Values
                .Where(entry => entry.Type == DependencyType.InstantiatedSingleton)
                .Select(entry => entry.Factory(null) as IDisposable)
                .Where(value => value != null))
            {
                    disposable.Dispose(); 
            }

            mEntries.Clear(); 
        }
    }
}
