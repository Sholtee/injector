﻿/********************************************************************************
* ProducibleServiceEntryFactory.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal partial class ProducibleServiceEntry
    {
        private static readonly IReadOnlyDictionary<Lifetime, Type> ServiceEntryTypes = Enum
            .GetValues(typeof(Lifetime))
            .Cast<Lifetime>()
            .Select(lt => new
            {
                Lifetime = lt,
                RelatedEntryKind = typeof(Lifetime)
                    .GetMember(lt.ToString())
                    .Single()
                    .GetCustomAttribute<RelatedEntryKindAttribute>()
            })
            .Where(lt => lt.RelatedEntryKind != null)
            .ToDictionary(lt => lt.Lifetime, lt => lt.RelatedEntryKind.ServiceEntry);

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
