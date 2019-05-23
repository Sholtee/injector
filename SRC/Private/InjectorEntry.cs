/********************************************************************************
* InjectorEntry.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    internal sealed class InjectorEntry: Disposable, ICloneable
    {
        /// <summary>
        /// Peldany gyar. Generikus implementacional (nem feltetlen) es Instance() hivasnal (biztosan) NULL.
        /// </summary>
        public Func<IInjector, Type, object> Factory { get; set; }

        /// <summary>
        /// A letrehozando peldany elettartama. NULL Instance(releaseOnDispose: false) hivassal regisztralt bejegyzeseknel.
        /// </summary>
        public Lifetime? Lifetime { get; set; }

        /// <summary>
        /// A bejegyzes kulcsa (lehet generikus).
        /// </summary>
        public Type Interface { get; set; }

        /// <summary>
        /// Az interface implementacioja (lehet generikus). Nem NULL szervizek eseten.
        /// </summary>
        public Type Implementation { get; set; }

        /// <summary>
        /// Legyartott (Lifetime.Singleton eseten) vagy kivulrol definialt (Instance() hivas) peldany, kulomben NULL. 
        /// </summary>
        public object Value { get; set; }

        public bool IsService => Implementation != null;

        public bool IsFactory => Implementation == null && Factory != null;

        public bool IsInstance => Implementation == null && Factory == null && Value != null;

        public object Clone()
        {
            CheckDisposed();

            return new InjectorEntry
            {
                Factory        = Factory,
                Implementation = Implementation,
                Interface      = Interface,

                //
                // Ha a peldany regisztralasakor a "releaseOnDispose" igazra volt allitva akkor
                // a peldany is lehet Singleton. Viszont mi nem akarjuk h a gyermek injektor
                // felszabadatisasakor is felszabaditasra keruljon a peldany ezert az elettartamot
                // nem masoljuk.
                //

                Lifetime = IsInstance ? null : Lifetime,

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