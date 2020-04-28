/********************************************************************************
* ServiceAlreadyRegisteredException.cs                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Internals;
    using Properties;

    /// <summary>
    /// The exception that is thrown when a service has already been registered.
    /// </summary>
    public sealed class ServiceAlreadyRegisteredException: Exception
    {
        /// <summary>
        /// Creates a new <see cref="ServiceAlreadyRegisteredException"/> instance.
        /// </summary>
        /// <param name="key">The "id" of the service.</param>
        internal ServiceAlreadyRegisteredException(IServiceId key): this(string.Format(Resources.Culture, Resources.ALREADY_REGISTERED, key.FriendlyName()))
        {
        }

        /// <summary>
        /// Creates a new <see cref="ServiceAlreadyRegisteredException"/> instance.
        /// </summary>
        public ServiceAlreadyRegisteredException()
        {
        }

        /// <summary>
        /// Creates a new <see cref="ServiceAlreadyRegisteredException"/> instance.
        /// </summary>
        public ServiceAlreadyRegisteredException(string message) : this(message, null!)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ServiceAlreadyRegisteredException"/> instance.
        /// </summary>
        public ServiceAlreadyRegisteredException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
