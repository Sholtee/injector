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

        protected ProducibleServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner) : base(@interface, name, factory, owner)
        {
        }

        protected ProducibleServiceEntry(Type @interface, string? name, Type implementation, IServiceContainer owner) : base(@interface, name, implementation, owner)
        {
        }

        protected ProducibleServiceEntry(Type @interface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner) : base(@interface, name, implementation, explicitArgs, owner)
        {
        }

        #region Features
        Func<IInjector, Type, object>? ISupportsProxying.Factory { get => Factory; set => Factory = value; }

        AbstractServiceEntry ISupportsSpecialization.Specialize(params Type[] genericArguments)
        {
            CheckNotDisposed();
            Ensure.Parameter.IsNotNull(genericArguments, nameof(genericArguments));

            return (
                this switch
                {
                //
                // "Service(typeof(IGeneric<>), ...)" eseten az implementaciot konkretizaljuk.
                //

                _ when Implementation is not null && ExplicitArgs is null =>
                        Lifetime!.CreateFrom(Interface.MakeGenericType(genericArguments), Name, Implementation.MakeGenericType(genericArguments), Owner),
                    _ when Implementation is not null && ExplicitArgs is not null =>
                        Lifetime!.CreateFrom(Interface.MakeGenericType(genericArguments), Name, Implementation.MakeGenericType(genericArguments), ExplicitArgs, Owner),

                //
                // "Factory(typeof(IGeneric<>), ...)" eseten az eredeti factory lesz hivva a 
                // konkretizalt interface-re.
                //

                _ when Factory is not null =>
                        Lifetime!.CreateFrom(Interface.MakeGenericType(genericArguments), Name, Factory, Owner),
                    _ => throw new NotSupportedException()
                }
            ).Single();
        }
        #endregion
    }
}