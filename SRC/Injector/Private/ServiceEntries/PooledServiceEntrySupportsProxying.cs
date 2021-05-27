/********************************************************************************
* PooledServiceEntrySupportsProxying.cs                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class PooledServiceEntrySupportsProxying : PooledServiceEntry, ISupportsProxying, ISupportsSpecialization
    {
        public PooledServiceEntrySupportsProxying(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner) : base(@interface, name, factory, owner)
        {
        }

        public PooledServiceEntrySupportsProxying(Type @interface, string? name, Type implementation, IServiceContainer owner) : base(@interface, name, implementation, owner)
        {
        }

        public PooledServiceEntrySupportsProxying(Type @interface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner) : base(@interface, name, implementation, explicitArgs, owner)
        {
        }

        Func<IInjector, Type, object>? ISupportsProxying.Factory { get => Factory; set => Factory = value; }

        AbstractServiceEntry ISupportsSpecialization.Specialize(params Type[] genericArguments) => this switch
        {
            //
            // Itt ne a "Lifetime"-ot hasznaljuk mert a pool-t nem szeretnenk megegyszer regisztralni.
            //

            _ when Implementation is not null && ExplicitArgs is null => new PooledServiceEntrySupportsProxying
            (
                Interface.MakeGenericType(genericArguments),
                Name,
                Implementation.MakeGenericType(genericArguments),
                Owner
            ),
            _ when Implementation is not null && ExplicitArgs is not null => new PooledServiceEntrySupportsProxying
            (
                Interface.MakeGenericType(genericArguments),
                Name,
                Implementation.MakeGenericType(genericArguments),
                ExplicitArgs,
                Owner
            ),
            _ when Factory is not null => new PooledServiceEntrySupportsProxying
            (
                Interface.MakeGenericType(genericArguments),
                Name,
                Factory,
                Owner
            ),
            _ => throw new NotSupportedException()
        };
    }
}