/********************************************************************************
* ServiceEntryExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    internal static partial class ServiceEntryExtensions
    {
        public static object GetOrCreateInstance(this AbstractServiceEntry entry, IServiceFactory factory)
        {
            object instance = factory.GetOrCreateInstance(entry);

            if (entry is IRequiresServiceAccess accessor)
                instance = accessor.ServiceAccess(instance);

            if (!entry.Interface.IsInstanceOfType(instance))
                throw new InvalidCastException(string.Format(Resources.Culture, Resources.INVALID_INSTANCE, entry.Interface));

            return instance;
        }
    }
}
