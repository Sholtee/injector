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
        /// Peldany gyar. NULL generikus implementaciohoz tartozo es Instance() hivassal regisztralt bejegyzeseknel.
        /// </summary>
        public Func<IInjector, Type, object> Factory { get; set; }

        /// <summary>
        /// A letrehozando peldany elettartama. NULL Instance() hivassal regisztralt bejegyzeseknel.
        /// </summary>
        public Lifetime? Lifetime { get; set; }

        /// <summary>
        /// A bejegyzes kulcsa (lehet generikus).
        /// </summary>
        public Type Interface { get; set; }

        /// <summary>
        /// Az interface implementacioja. NULL Factory() es Instance() hivassal regisztralt bejegyzeseknel.
        /// </summary>
        public Type Implementation { get; set; }

        /// <summary>
        /// Legyartott (Lifetime.Singleton eseten) vagy kivulrol definialt (Instance() hivas) peldany, kulomben NULL. 
        /// </summary>
        public object Value { get; set; }

        public object Clone()
        {
            CheckDisposed();

            return new InjectorEntry
            {
                Factory        = Factory,
                Implementation = Implementation,
                Interface      = Interface,
                Lifetime       = Lifetime,

                //
                // Az ertekek keruljenek ujra legyartasra kiveve ha Instance() hivassal
                // kerultek regisztralasra.
                //

                Value = Lifetime == null ? Value : null         
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