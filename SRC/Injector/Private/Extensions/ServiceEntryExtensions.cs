/********************************************************************************
* ServiceEntryExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal static partial class ServiceEntryExtensions
    {
        //
        // Dictionary performs much better against int keys
        //

        private static readonly ConcurrentDictionary<int, AbstractServiceEntry> FSpecializedEntries = new();

        //
        // Always return the same specialized entry to not screw up the circular reference validation.
        //

        public static AbstractServiceEntry Specialize(this AbstractServiceEntry entry, Type iface) => FSpecializedEntries.GetOrAdd
        (
            unchecked(entry.GetHashCode() ^ iface.GetHashCode()),
            _ => ((ISupportsSpecialization) entry).Specialize(iface.GenericTypeArguments)
        );
    }
}
