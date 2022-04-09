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
    using Properties;

    internal static partial class ServiceEntryExtensions
    {
        public static object GetOrCreateInstance(this AbstractServiceEntry entry, IServiceFactory factory) // TODO: remove
        {
            object instance = factory.GetOrCreateInstance(entry);

            if (entry is IRequiresServiceAccess accessor)
                instance = accessor.ServiceAccess(instance);

            if (!entry.Interface.IsInstanceOfType(instance))
                throw new InvalidCastException(string.Format(Resources.Culture, Resources.INVALID_INSTANCE, entry.Interface));

            return instance;
        }

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
            _ => ((ISupportsSpecialization) entry).Specialize(null!, iface.GenericTypeArguments)
        );
    }
}
