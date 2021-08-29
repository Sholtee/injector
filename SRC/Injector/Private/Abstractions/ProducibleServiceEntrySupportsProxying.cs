/********************************************************************************
* ProducibleServiceEntrySupportsProxying.cs                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    /// <summary>
    /// Describes a producible service entry.
    /// </summary>
    internal abstract class ProducibleServiceEntrySupportsProxying : ProducibleServiceEntry, ISupportsProxying
    {
        protected ProducibleServiceEntrySupportsProxying(ProducibleServiceEntrySupportsProxying entry, IServiceContainer owner) : base(entry, owner)
        {
        }

        protected ProducibleServiceEntrySupportsProxying(ProducibleServiceEntrySupportsProxying entry, IServiceRegistry owner) : base(entry, owner)
        {
        }

        protected ProducibleServiceEntrySupportsProxying(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner) : base(@interface, name, factory, owner)
        {
        }

        protected ProducibleServiceEntrySupportsProxying(Type @interface, string? name, Type implementation, IServiceContainer owner) : base(@interface, name, implementation, owner)
        {
        }

        protected ProducibleServiceEntrySupportsProxying(Type @interface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner) : base(@interface, name, implementation, explicitArgs, owner)
        {
        }

        Func<IInjector, Type, object>? ISupportsProxying.Factory { get => Factory; set => Factory = value; }
    }
}