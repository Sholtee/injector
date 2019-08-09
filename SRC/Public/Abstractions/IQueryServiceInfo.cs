/********************************************************************************
* IQueryServiceInfo.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI
{
    using Internals;

    /// <summary>
    /// Provides the layout of <see cref="QueryServiceInfo"/>.
    /// </summary>
    public interface IQueryServiceInfo
    {
        /// <summary>
        /// Gets basic informations about a registered service.
        /// </summary>
        /// <param name="iface">The "id" of the service to be queried. It must be an interface.</param>
        /// <returns>An <see cref="IServiceInfo"/> instance.</returns>
        /// <exception cref="NotSupportedException">The service can not be found.</exception>
        IServiceInfo QueryServiceInfo([ParameterIs(typeof(NotNull), typeof(Interface))] Type iface);

        /// <summary>
        /// Registered entries.
        /// </summary>
        IReadOnlyCollection<IServiceInfo> Entries { get; }
    }
}