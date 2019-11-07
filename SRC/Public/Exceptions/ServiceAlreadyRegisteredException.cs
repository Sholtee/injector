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
    public sealed class ServiceAlreadyRegisteredException: ArgumentException
    {
        /// <summary>
        /// Creates a new <see cref="ServiceAlreadyRegisteredException"/> instance.
        /// </summary>
        /// <param name="key">The "id" of the service.</param>
        /// <param name="innerException">The inner exception (if it is present).</param>
        public ServiceAlreadyRegisteredException((Type Interface, string Name) key, Exception innerException = null): base(string.Format(Resources.ALREADY_REGISTERED, key.FriendlyName()), innerException)
        {
        }
    }
}
