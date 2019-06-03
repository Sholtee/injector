/********************************************************************************
* InjectorEntry.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal sealed class InjectorEntry: Disposable, IServiceInfo, ICloneable
    {
        private Func<IInjector, Type, object> FFactory;
        private object FValue;
        private readonly ITypeResolver FImplementation;

        private sealed class DefaultTypeResolver : ITypeResolver
        {
            private Type Interface { get; }
            private Type Implementation { get; }

            public DefaultTypeResolver(Type @interface, Type implementation)
            {
                Interface      = @interface;
                Implementation = implementation;
            }

            Type ITypeResolver.Resolve(Type @interface)
            {
                Debug.Assert(@interface == Interface);
                return Implementation;
            }
        }

        #region Immutables
        /// <summary>
        /// The key of the entry (can be open generic type).
        /// </summary>
        public Type Interface { get; }

        /// <summary>
        /// The service implementation (can be open generic type). Not NULL in the case of services.
        /// </summary>
        /// <remarks>If the entry belongs to a lazy service, getting this property will trigger the type resolver.</remarks>
        public Type Implementation => FImplementation?.Resolve(Interface);

        /// <summary>
        /// The lifetime of the entity to be created. NULL if the entry was created by an "Instance(releaseOnDispose: false)" call.
        /// </summary>
        public Lifetime? Lifetime { get; }

        public bool IsService => FImplementation != null;

        public bool IsLazy => IsService && !(FImplementation is DefaultTypeResolver);

        public bool IsFactory => !IsService && Factory != null;

        public bool IsInstance => !IsService && !IsFactory && Value != null;
        #endregion

        #region Mutables
        /// <summary>
        /// Peldany gyar. Generikus implementacional (nem feltetlen) es Instance() hivasnal (biztosan) NULL.
        /// </summary>
        public Func<IInjector, Type, object> Factory
        {
            get => FFactory;
            set
            {
                if (FFactory != null && value == null)
                    throw new ArgumentNullException(nameof(value));
                FFactory = value;
            }
        }

        /// <summary>
        /// Legyartott (Lifetime.Singleton eseten) vagy kivulrol definialt (Instance() hivas) peldany, kulomben NULL. 
        /// </summary>
        public object Value
        {
            get => FValue;
            set
            {
                if (FValue != null)
                    throw new InvalidOperationException(Resources.MULTIPLE_ASSIGN);
                FValue = value;
            }
        }
        #endregion

        public InjectorEntry(Type @interface, Type implementation = null, Lifetime? lifetime = null): this
        (
            @interface, 
            implementation != null ? new DefaultTypeResolver(@interface, implementation) : null, 
            lifetime
        ) {}

        public InjectorEntry(Type @interface, ITypeResolver implementation, Lifetime? lifetime = null)
        {
            Interface = @interface;
            Lifetime  = lifetime;

            FImplementation = implementation;
        }

        public object Clone()
        {
            CheckDisposed();

            if (Value is IInjector)
                throw new InvalidOperationException(Resources.CANT_CLONE);

            //
            // 1) Hogy klonozaskor ne legyen a TypeResolver triggerelve ezert magat a resolvert
            //    adjuk at.
            //
            // 2) Ha a peldany regisztralasakor a "releaseOnDispose" igazra volt allitva akkor
            //    a peldany is lehet Singleton. Viszont mi nem akarjuk h a gyermek injektor
            //    felszabadatisasakor is dispose-olva legyen a peldany ezert az elettartamot
            //    nem masoljuk.
            //

            return new InjectorEntry(Interface, FImplementation, IsInstance ? null : Lifetime)
            {
                Factory = Factory,

                //
                // 3) Az ertekek keruljenek ujra legyartasra kiveve ha Instance() hivassal
                //    kerultek regisztralasra.
                //

                Value = IsInstance ? Value : null         
            };
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