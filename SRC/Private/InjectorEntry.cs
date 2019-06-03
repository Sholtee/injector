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
        private readonly Func<Type> FGetImplementation;

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
        /// A bejegyzes kulcsa (lehet generikus).
        /// </summary>
        public Type Interface { get; }

        /// <summary>
        /// Az interface implementacioja (lehet generikus). Nem NULL szervizek eseten.
        /// </summary>
        public Type Implementation => FGetImplementation();

        /// <summary>
        /// A letrehozando peldany elettartama. NULL Instance(releaseOnDispose: false) hivassal regisztralt bejegyzeseknel.
        /// </summary>
        public Lifetime? Lifetime { get; }

        public bool IsService => Implementation != null;

        public bool IsLazy => IsService && Implementation == typeof(ITypeResolver);

        public bool IsFactory => Implementation == null && Factory != null;

        public bool IsInstance => Implementation == null && Factory == null && Value != null;
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

        public InjectorEntry(Type @interface, Type implementation = null, Lifetime? lifetime = null): this(@interface, new DefaultTypeResolver(@interface, implementation), lifetime)
        {
        }

        public InjectorEntry(Type @interface, ITypeResolver implementation, Lifetime? lifetime = null)
        {
            Interface = @interface;
            Lifetime  = lifetime;

            FGetImplementation = () => implementation.Resolve(@interface);
        }

        public object Clone()
        {
            CheckDisposed();

            if (Value is IInjector)
                throw new InvalidOperationException(Resources.CANT_CLONE);

            //
            // Ha a peldany regisztralasakor a "releaseOnDispose" igazra volt allitva akkor
            // a peldany is lehet Singleton. Viszont mi nem akarjuk h a gyermek injektor
            // felszabadatisasakor is dispose-olva legyen a peldany ezert az elettartamot
            // nem masoljuk.
            //

            return new InjectorEntry(Interface, Implementation, IsInstance ? null : Lifetime)
            {
                Factory = Factory,

                //
                // Az ertekek keruljenek ujra legyartasra kiveve ha Instance() hivassal
                // kerultek regisztralasra.
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