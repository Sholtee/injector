/********************************************************************************
* ProducibleServiceEntryFactory.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    internal partial class ProducibleServiceEntry
    {
        private static readonly IReadOnlyDictionary<Lifetime, Type> ServiceEntryTypes = new Dictionary<Lifetime, Type> 
        {
            {Interfaces.Lifetime.Transient, typeof(TransientServiceEntry)},
            {Interfaces.Lifetime.Scoped, typeof(ScopedServiceEntry)},
            {Interfaces.Lifetime.Singleton, typeof(SingletonServiceEntry)}
        };

        public static ProducibleServiceEntry Create<TParam>(Lifetime? lifetime, Type @interface, string? name, TParam param, IServiceContainer owner)
        {
            if (lifetime == null || !ServiceEntryTypes.TryGetValue(lifetime.Value, out var serviceEntryType))
                throw new ArgumentException(string.Format(Resources.Culture, Resources.UNKNOWN_LIFETIME, lifetime ?? (object) "NULL"), nameof(lifetime));                
        
            return (ProducibleServiceEntry) serviceEntryType.CreateInstance
            (
                new[]
                {
                    typeof(Type), // interface
                    typeof(string), // name
                    typeof(TParam),
                    typeof(IServiceContainer) // owner
                }, 
                @interface,
                name,
                param,
                owner
            );
        }
    }
}
