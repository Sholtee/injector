/********************************************************************************
* CompositeKey.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class CompositeKey: IServiceId
    {
        public CompositeKey(Type iface, string? name)
        {
            Interface = iface;
            Name = name;
        }

        public Type Interface { get; }

        public string? Name { get; }
    }
}
