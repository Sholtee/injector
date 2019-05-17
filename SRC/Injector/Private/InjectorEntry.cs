/********************************************************************************
* InjectorEntry.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using JetBrains.Annotations;

namespace Solti.Utils.DI
{
    internal sealed class InjectorEntry
    {
        /// <summary>
        /// Peldany gyar. NULL generikus implementaciohoz tartozo es Instance() hivassal regisztralt bejegyzeseknel.
        /// </summary>
        public Func<Type, object> Factory { get; set; }

        /// <summary>
        /// A letrehozando peldany elettartama. NULL Instance() hivassal regisztralt bejegyzeseknel.
        /// </summary>
        public Lifetime? Lifetime { get; set; }

        [NotNull]
        public Type Interface { get; set; }

        /// <summary>
        /// Az interface implementacioja. NULL Factory() es Instance() hivassal regisztralt bejegyzeseknel.
        /// </summary>
        public Type Implementation { get; set; }

        /// <summary>
        /// Legyartott (Lifetime.Singleton eseten) vagy kivulrol definialt (Instance() hivas) peldany. 
        /// </summary>
        public object Value { get; set; }
    }
}