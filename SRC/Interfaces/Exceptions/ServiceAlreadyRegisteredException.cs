/********************************************************************************
* ServiceAlreadyRegisteredException.cs                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// The exception that is thrown on duplicate service registration.
    /// </summary>
    public sealed class ServiceAlreadyRegisteredException: Exception
    {
        /// <summary>
        /// Creates a new <see cref="ServiceAlreadyRegisteredException"/> instance.
        /// </summary>
        public ServiceAlreadyRegisteredException(string message, AbstractServiceEntry entry) : base(message)
            => Entry = entry;

        /// <summary>
        /// The service descriptor whose registration was failed.
        /// </summary>
        public AbstractServiceEntry Entry { get; }
    }
}
