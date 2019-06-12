/********************************************************************************
* InjectorEntry.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal sealed class InjectorEntry: Disposable, IServiceInfo, ICloneable // TODO: !!TESTS!!
    {
        private readonly Lazy<Type> FImplementation;
        private object FValue;

        #region Immutables (IServiceInfo)
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

        private InjectorEntry(Type @interface, Lazy<Type> implementation, Lifetime? lifetime, bool isLazy)
        {
            Interface = @interface;
            Lifetime  = lifetime;
            IsLazy    = isLazy;

            FImplementation = implementation;
        }

        public InjectorEntry(Type @interface, Type implementation = null, Lifetime? lifetime = null): 
            this(@interface, implementation != null ? new Lazy<Type>(() => implementation) : null, lifetime, false) {}

        public InjectorEntry(Type @interface, ITypeResolver resolver, Lifetime? lifetime = null): 
            this(@interface, new Lazy<Type>(() => resolver.Resolve(@interface), LazyThreadSafetyMode.ExecutionAndPublication), lifetime, true) {}

        public object Clone()
        {
            CheckDisposed();

            if (Value is IInjector)
                throw new InvalidOperationException(Resources.CANT_CLONE);

            //
            // 1) Hogy klonozaskor egyaltalan ne es kesobb is maximum csak egyszer legyen a TypeResolver 
            //    triggerelve ezert magat a Lazy<> peldanyt adjuk at.
            //
            // 2) Ha a peldany regisztralasakor a "releaseOnDispose" igazra volt allitva akkor
            //    a peldany is lehet Singleton. Viszont mi nem akarjuk h a gyermek injektor
            //    felszabadatisasakor is dispose-olva legyen a peldany ezert az elettartamot
            //    nem masoljuk.
            //

            return new InjectorEntry(Interface, FImplementation, IsInstance ? null : Lifetime, IsLazy)
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