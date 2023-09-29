/********************************************************************************
* IServiceId.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes an abstract service identifier.
    /// </summary>
    /// <remarks>A service can be identified uniquely by its <see cref="Interface"/> and <see cref="Name"/></remarks>
    public interface IServiceId
    {
        /// <summary>
        /// The service interface.
        /// </summary>
        Type Interface { get; }

        /// <summary>
        /// The (optional) service name.
        /// </summary>
        object? Name { get; }
    }
}