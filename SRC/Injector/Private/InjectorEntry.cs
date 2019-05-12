/********************************************************************************
* InjectorEntry.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.Injector
{
    internal sealed class InjectorEntry
    {
        public Func<IReadOnlyList<Type>, object> Factory { get; set; }
        public DependencyType Type { get; set; }
        public Type Interface { get; set; }
        public Type GenericImplementation { get; set; }
    }
}