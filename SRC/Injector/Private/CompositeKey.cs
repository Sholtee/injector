/********************************************************************************
* ServicePath.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using static System.StringComparer;

namespace Solti.Utils.DI.Internals
{
    internal sealed class CompositeKey: IComparable<CompositeKey>, IEquatable<CompositeKey>
    {
        private int? FHash;

        private readonly long FHandle;

        private readonly string? FName;

        public CompositeKey(Type iface, string? name)
        {
            FHandle = (long) iface.TypeHandle.Value;
            FName = name;   
        }

        public override int GetHashCode() =>
            //
            // We don't need lock in multithreaded environments as assigning a value to FHash multiple times
            // should not cause any issue.
            //

            #pragma warning disable CA1307
            FHash ??= unchecked(FHandle.GetHashCode() ^ (FName?.GetHashCode() ?? 0));
            #pragma warning restore CA1307

        public override bool Equals(object obj) => obj is CompositeKey other && Equals(other);

        public bool Equals(CompositeKey other) => FHandle == other.FHandle && FName == other.FName;

        public int CompareTo(CompositeKey other)
        {
            //
            // We have to return Int32 -> Math.Sign()
            //

            int order = Math.Sign(FHandle - other.FHandle);
            if (order is 0)
                //
                // StringComparer supports NULL despite it is not reflected by nullable annotation
                //

                order = InvariantCultureIgnoreCase.Compare(FName, other.FName);
            return order;
        }
    }
}
