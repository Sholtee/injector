/********************************************************************************
* ProducibleServiceEntryFactory.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal static class ProducibleServiceEntryFactory
    {
        public static ProducibleServiceEntry CreateEntry<TParam>(Lifetime? lifetime, Type @interface, TParam param, IServiceContainer owner)
        {
            Type serviceEntryType;

            switch (lifetime)
            {
                case Lifetime.Singleton:
                    serviceEntryType = typeof(SingletonServiceEntry);
                    break;
                case Lifetime.Scoped:
                    serviceEntryType = typeof(ScopedServiceEntry);
                    break;
                case Lifetime.Transient:
                    serviceEntryType = typeof(TransientServiceEntry);
                    break;
                default:
                    throw new ArgumentException(string.Format(Resources.UNKNOWN_LIFETIME, lifetime), nameof(lifetime));                  
            }

            return (ProducibleServiceEntry) serviceEntryType.CreateInstance
            (
                new[]
                {
                    typeof(Type), // interface
                    typeof(TParam),
                    typeof(IServiceContainer) // owner
                }, 
                @interface, 
                param,
                owner
            );
        }
    }
}
