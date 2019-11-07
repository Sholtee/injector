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
        public ServiceNotFoundException((Type Interface, string Name) key) : base(string.Format(Resources.SERVICE_NOT_FOUND, key.FriendlyName()))
        {
        }
    }
}
