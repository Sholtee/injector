/********************************************************************************
* IServiceId.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes a service identifier.
    /// </summary>
    public interface IServiceId
    {
        /// <summary>
        /// The service interface.
        /// </summary>
        Type Interface { get; }

        /// <summary>
        /// The (optional) service name.
        /// </summary>
        string? Name { get; }
    }
}