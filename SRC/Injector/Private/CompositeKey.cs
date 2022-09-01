/********************************************************************************
* ServicePath.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using static System.StringComparer;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class CompositeKey: IComparable<CompositeKey>, IEquatable<CompositeKey>
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

        public CompositeKey(AbstractServiceEntry entry) : this(entry.Interface, entry.Name) { }

        public override int GetHashCode() => Hash;

        public override bool Equals(object obj) => obj is CompositeKey other && Equals(other);

        public bool Equals(CompositeKey other) => Handle == other.Handle && Name == other.Name;

        public int CompareTo(CompositeKey other)
        {
            //
            // We have to return Int32 -> Math.Sign()
            //

            int order = Math.Sign(Handle - other.Handle);
            if (order is 0)
                //
                // StringComparer supports NULL despite it is not reflected by nullable annotation
                //

                order = InvariantCultureIgnoreCase.Compare(Name, other.Name);
            return order;
        }
    }
}
