/********************************************************************************
* FactoryOptions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    /// <summary>
    /// Controls the factory during the service creation.
    /// </summary>
    [Flags]
    public enum FactoryOptions
    {
        /// <summary>
        /// Default
        /// </summary>
        None = 0,

        /// <summary>
        /// Forces the system to manage the lifetime of the newly created service (if possible).
        /// </summary>
        ForceAutoDispose
    }
}
