/********************************************************************************
* QueryModes.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Specifies flags that control the <see cref="IServiceContainer.Get"/> method.
    /// </summary>
    [Flags]
    public enum QueryModes
    {
        /// <summary>
        /// Allows returning NULL if the desired service could not be found.
        /// </summary>
        #pragma warning disable CA1008 // "Default" is the zero value
        Default = 0,
        #pragma warning restore CA1008

        /// <summary>
        /// Instructs the <see cref="IServiceContainer.Get"/> to throw if the desired service could not be found.
        /// </summary>
        ThrowOnMissing,

        /// <summary>
        /// Allows "auto registering" if the entry can be specialized.
        /// </summary>
        AllowSpecialization
    }
}