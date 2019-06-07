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

    internal sealed class InjectorEntry: Disposable, IServiceInfo, ICloneable // TODO: !!TESTS!!
    {
        private Func<IInjector, Type, object> FFactory;
        private Type FImplementation;
        private object FValue;
        private readonly ITypeResolver FResolver;

        //
        // Kidelegalja a tipus lekerdezeset a szulo entitasnak. Ez a trukk arra van h ha klonozzuk a 
        // bejegyzest a resolver akkor is csak egyszer keruljon meghivasra fuggetlenul attol melyik 
        // bejegyzesben volt hivatkozva a tipus.
        //

        private sealed class DelegateTypeResolver : ITypeResolver
        {
            private InjectorEntry Parent { get; }

            public DelegateTypeResolver(InjectorEntry parent) => Parent = parent;

            Type ITypeResolver.Resolve(Type @interface)
            {
                Debug.Assert(@interface == Parent.Interface);
                return Parent.Implementation;
            }
        }

        #region Immutables (IServiceInfo)
        public Type Interface { get; }

        public Type Implementation
        {
            get
            {
                if (FImplementation == null)
                {
                    if (FResolver != null) lock (FResolver)
                    {
                        if (FImplementation == null) FImplementation = FResolver.Resolve(Interface);
                    }
                }
                return FImplementation;
            }
        }

        public Lifetime? Lifetime { get; }

        public bool IsService => FImplementation != null || FResolver != null;

        public bool IsLazy => FResolver != null;

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

        public InjectorEntry(Type @interface, Type implementation = null, Lifetime? lifetime = null)
        {
            Interface = @interface;
            Lifetime = lifetime;

            FImplementation = implementation;
        }

        public InjectorEntry(Type @interface, ITypeResolver resolver, Lifetime? lifetime = null)
        {
            Interface = @interface;
            Lifetime  = lifetime;

            FResolver = resolver;
        }

        public object Clone()
        {
            CheckDisposed();

            if (Value is IInjector)
                throw new InvalidOperationException(Resources.CANT_CLONE);

            //
            // 1) Hogy klonozaskor egyaltalan ne es kesobb is maximum csak egyszer legyen a TypeResolver 
            //    triggerelve ezert sajat resolvert adunk at.
            //
            // 2) Ha a peldany regisztralasakor a "releaseOnDispose" igazra volt allitva akkor
            //    a peldany is lehet Singleton. Viszont mi nem akarjuk h a gyermek injektor
            //    felszabadatisasakor is dispose-olva legyen a peldany ezert az elettartamot
            //    nem masoljuk.
            //

            return new InjectorEntry(Interface, new DelegateTypeResolver(this), IsInstance ? null : Lifetime)
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