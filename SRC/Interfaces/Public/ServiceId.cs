/********************************************************************************
* ServiceId.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    using Primitives;

    /// <summary>
    /// Default <see cref="IServiceId"/> implementation
    /// </summary>
    public sealed class ServiceId : IServiceId
    {
        /// <summary>
        /// Creates a new <see cref="ServiceId"/> instance.
        /// </summary>
        public ServiceId(Type iface, object? name)
        {
            Interface = iface ?? throw new ArgumentNullException(nameof(iface));
            Name = name;
        }

        /// <summary>
        /// The service interface.
        /// </summary>
        public Type Interface { get; }

        /// <summary>
        /// The (optional) service name.
        /// </summary>
        public object? Name { get; }

        /// <inheritdoc/>
        public override string ToString() => Interface.GetFriendlyName() + Name is not null ? $":{Name}" : string.Empty;
    }
}
