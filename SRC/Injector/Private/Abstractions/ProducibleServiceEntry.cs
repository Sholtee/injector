/********************************************************************************
* ProducibleServiceEntry.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    /// <summary>
    /// Describes a producible service entry.
    /// </summary>
    internal abstract class ProducibleServiceEntry : ProducibleServiceEntryBase, ISupportsProxying, ISupportsSpecialization
    {
        protected ProducibleServiceEntry(ProducibleServiceEntry entry, IServiceContainer owner) : base(entry, owner)
        {
        }

        protected ProducibleServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner, params Func<object, Type, object>[] customConverters) : base(@interface, name, factory, owner, customConverters)
        {
        }

        protected ProducibleServiceEntry(Type @interface, string? name, Type implementation, IServiceContainer owner, params Func<object, Type, object>[] customConverters) : base(@interface, name, implementation, owner, customConverters)
        {
        }

        protected ProducibleServiceEntry(Type @interface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner, params Func<object, Type, object>[] customConverters) : base(@interface, name, implementation, explicitArgs, owner, customConverters)
        {
        }

        #region Features
        Func<IInjector, Type, object>? ISupportsProxying.Factory { get => Factory; set => Factory = value; }

        AbstractServiceEntry ISupportsSpecialization.Specialize(params Type[] genericArguments) =>
        (
            this switch
            {
                //
                // "Service(typeof(IGeneric<>), ...)" eseten az implementaciot konkretizaljuk.
                //

                _ when Implementation is not null && ExplicitArgs is null =>
                    Lifetime!.CreateFrom(Interface.MakeGenericType(genericArguments), Name, Implementation.MakeGenericType(genericArguments), Owner, CustomConverters.ToArray()),
                _ when Implementation is not null && ExplicitArgs is not null =>
                    Lifetime!.CreateFrom(Interface.MakeGenericType(genericArguments), Name, Implementation.MakeGenericType(genericArguments), ExplicitArgs, Owner, CustomConverters.ToArray()),

                //
                // "Factory(typeof(IGeneric<>), ...)" eseten az eredeti factory lesz hivva a 
                // konkretizalt interface-re.
                //

                _ when Factory is not null =>
                    Lifetime!.CreateFrom(Interface.MakeGenericType(genericArguments), Name, Factory, Owner, CustomConverters.ToArray()),
                _ => throw new NotSupportedException()
            }
        ).Single();
        #endregion
    }
}