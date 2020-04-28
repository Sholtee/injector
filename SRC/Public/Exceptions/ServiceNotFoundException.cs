/********************************************************************************
* ServiceNotFoundException.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Internals;
    using Properties;

    /// <summary>
    /// The exception that is thrown when a service could not be found.
    /// </summary>
    public sealed class ServiceNotFoundException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="ServiceNotFoundException"/> instance.
        /// </summary>
        /// <param name="key">The "id" of the service that could not be found.</param>
        internal ServiceNotFoundException(IServiceId key) : this(string.Format(Resources.Culture, Resources.SERVICE_NOT_FOUND, key.FriendlyName()))
        {
        }

        /// <summary>
        /// Creates a new <see cref="ServiceNotFoundException"/> instance.
        /// </summary>
        public ServiceNotFoundException()
        {
        }

        /// <summary>
        /// Creates a new <see cref="ServiceNotFoundException"/> instance.
        /// </summary>
        public ServiceNotFoundException(string message) : this(message, null!)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ServiceNotFoundException"/> instance.
        /// </summary>
        public ServiceNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
