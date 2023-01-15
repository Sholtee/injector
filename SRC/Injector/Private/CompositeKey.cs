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

        public override int GetHashCode() => unchecked(Interface.GetHashCode() ^ (Name?.GetHashCode() ?? 0));

        public override bool Equals(object obj) => obj is CompositeKey other && other.Interface == Interface && other.Name == Name;
    }
}
