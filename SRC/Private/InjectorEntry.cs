/********************************************************************************
* InjectorEntry.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    internal sealed class InjectorEntry: ICloneable
    {
        /// <summary>
        /// Peldany gyar. NULL generikus implementaciohoz tartozo es Instance() hivassal regisztralt bejegyzeseknel.
        /// </summary>
        public Func<Type, object> Factory { get; set; }

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
    }
}