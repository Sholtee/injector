/********************************************************************************
* ServiceNotFoundException.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Properties;

    /// <summary>
    /// The exception that is thrown when a service could not be found.
    /// </summary>
    public sealed class ServiceNotFoundException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="ServiceNotFoundException"/> instance.
        /// </summary>
        /// <param name="iface">The service that could not be found.</param>
        public ServiceNotFoundException(Type iface): base(string.Format(Resources.SERVICE_NOT_FOUND, iface))
        {
        }
    }
}
