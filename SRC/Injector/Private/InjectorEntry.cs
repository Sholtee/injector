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
        public Func<Type, object> Factory { get; set; }
        public Lifetime Lifetime { get; set; }
        [NotNull]
        public Type Interface { get; set; }
        public Type Implementation { get; set; }
        public object Value { get; set; }
    }
}