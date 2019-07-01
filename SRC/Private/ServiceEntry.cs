/********************************************************************************
* ServiceEntry.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    /// <summary>
    /// Stores a service definition.
    /// </summary>
    /// <remarks>This is an internal class so it may change from version to version. Don't use it!</remarks>
    public sealed class ServiceEntry: Disposable, IServiceInfo, ICloneable // TODO: !!TESTS!!
    {
        private readonly Lazy<Type> FImplementation;
        private object FValue;

        #region Immutables
        public Type Interface { get; }

        public Type Implementation => FImplementation.Value;

        public Lifetime? Lifetime { get; }

        public bool IsService => FImplementation != null;

        public bool IsLazy { get; }

        public bool IsFactory => !IsService && Factory != null;

        public bool IsInstance => !IsService && !IsFactory && Value != null;
        #endregion

        #region Mutables
        /// <summary>
        /// Peldany gyar. Generikus implementacional (nem feltetlen) es Instance() hivasnal (biztosan) NULL.
        /// </summary>
        public Func<IInjector, Type, object> Factory { get; set; }

        /// <summary>
        /// Legyartott (Lifetime.Singleton eseten) vagy kivulrol definialt (Instance() hivas) peldany, kulomben NULL. 
        /// </summary>
        public object Value
        {
            get => FValue;
            set => FValue = FValue == null ? value : throw new InvalidOperationException(Resources.MULTIPLE_ASSIGN);
        }
        #endregion

        private ServiceEntry(Type @interface, Lazy<Type> implementation, Lifetime? lifetime, bool isLazy)
        {
            Interface = @interface;
            Lifetime  = lifetime;
            IsLazy    = isLazy;

            FImplementation = implementation;
        }

        public ServiceEntry(Type @interface, Type implementation = null, Lifetime? lifetime = null): 
            this(@interface, implementation != null ? new Lazy<Type>(() => implementation) : null, lifetime, false) {}

        public ServiceEntry(Type @interface, ITypeResolver resolver, Lifetime? lifetime = null): 
            this(@interface, new Lazy<Type>(() => resolver.Resolve(@interface), LazyThreadSafetyMode.ExecutionAndPublication), lifetime, true) {}

        /// <summary>
        /// See <see cref="ICloneable"/>.
        /// </summary>
        public object Clone()
        {
            CheckDisposed();

            if (Value is IInjector)
                //
                // Ide csak akkor jutunk ha Injector peldanyt akarnank szulo kontenerkent hasznalni.
                //

                throw new InvalidOperationException(Resources.CANT_CLONE);

            //
            // 1) Hogy klonozaskor egyaltalan ne es kesobb is maximum csak egyszer legyen a TypeResolver 
            //    triggerelve ezert magat a Lazy<> peldanyt adjuk at.
            //
            // 2) Ha a peldany regisztralasakor a "releaseOnDispose" igazra volt allitva akkor
            //    a peldany is lehet Singleton. Viszont mi nem akarjuk h a gyermek kontener
            //    felszabadatisasakor is dispose-olva legyen a peldany ezert az elettartamot
            //    nem masoljuk.
            //

            return new ServiceEntry(Interface, FImplementation, IsInstance ? null : Lifetime, IsLazy)
            {
                Factory = Factory,

                //
                // 3) Az ertekek keruljenek ujra legyartasra kiveve ha Instance() hivassal
                //    kerultek regisztralasra.
                //    Mondjuk ez jelen esetbe nem nagyon jatszik tekintve h Injector peldanyt
                //    nem hasznalunk szulo kontenernek.
                //

                Value = IsInstance ? Value : null
            };
        }

        /// <summary>
        /// Creates a new specialized version from this entry.
        /// </summary>
        public ServiceEntry Specialize(params Type[] genericArguments)
        {
            var specialied = new ServiceEntry(Interface.MakeGenericType(genericArguments), Implementation.MakeGenericType(genericArguments), Lifetime);
            specialied.SetFactory();

            return specialied;
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                //
                // Csak a Singleton eletciklusu entitasokat kell felszabaditsuk.
                //

                if (Lifetime == DI.Lifetime.Singleton)
                {
                    (Value as IDisposable)?.Dispose();
                }
            }

            base.Dispose(disposeManaged);
        }
    }
}