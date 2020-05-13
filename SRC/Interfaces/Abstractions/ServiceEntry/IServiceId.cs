/********************************************************************************
* IServiceId.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes a service id.
    /// </summary>
    public interface IServiceId
    {
        /// <summary>
        /// The interface of the service.
        /// </summary>
        [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "The name will not confuse the users.")]
        Type Interface { get; }

        /// <summary>
        /// The name of the service.
        /// </summary>
        string? Name { get; }
    }
}