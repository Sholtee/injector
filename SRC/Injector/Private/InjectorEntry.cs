/********************************************************************************
* InjectorEntry.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    internal sealed class InjectorEntry
    {
        public Func<object> Factory { get; set; }
        public Lifetime Lifetime { get; set; }
        public Type Interface { get; set; }
        public Type Implementation { get; set; }
        public object Value { get; set; }
    }
}