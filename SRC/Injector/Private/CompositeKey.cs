/********************************************************************************
* ServicePath.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class CompositeKey
    {
        public readonly long Handle;

        public readonly string? Name;

        public readonly int Hash;

        public CompositeKey(Type iface, string? name)
        {
            Handle = (long) iface.TypeHandle.Value;
            Name = name;
            #pragma warning disable CA1307
            Hash = unchecked(Handle.GetHashCode() ^ (Name?.GetHashCode() ?? 0));
            #pragma warning restore CA1307
        }

        public override int GetHashCode() => Hash;

        public override bool Equals(object obj) =>
            obj is CompositeKey that && that.Handle == Handle && that.Name == Name;

        public CompositeKey(AbstractServiceEntry entry) : this(entry.Interface, entry.Name) { }
    }
}
