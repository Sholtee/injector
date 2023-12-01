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
        public ServiceId(Type type, object? key)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Key = key;
        }

        /// <summary>
        /// The service interface.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// The (optional) service name.
        /// </summary>
        public object? Key { get; }

        /// <inheritdoc/>
        public override string ToString() => Type.GetFriendlyName() + Key is not null ? $":{Key}" : string.Empty;
    }
}
