﻿/********************************************************************************
* ServiceAlreadyRegisteredException.cs                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Properties;

    /// <summary>
    /// The exception that is thrown when a service has already been registered.
    /// </summary>
    public sealed class ServiceAlreadyRegisteredException: ArgumentException
    {
        /// <summary>
        /// Creates a new <see cref="ServiceAlreadyRegisteredException"/> instance.
        /// </summary>
        /// <param name="iface">The interface which already was registered.</param>
        /// <param name="innerException">The inner exception (if present).</param>
        public ServiceAlreadyRegisteredException(Type iface, Exception innerException = null): base(string.Format(Resources.ALREADY_REGISTERED, iface), innerException)
        {
        }
    }
}
