/********************************************************************************
* QueryMode.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    /// <summary>
    /// Specifies flags that control the <see cref="IServiceContainer.Get"/> method.
    /// </summary>
    [Flags]
    public enum QueryMode
    {
        /// <summary>
        /// Allows returning NULL if the desired service could not be found.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Instructs the <see cref="IServiceContainer.Get"/> to throw if the desired service could not be found.
        /// </summary>
        ThrowOnError,

        /// <summary>
        /// Allows "auto registering" if the entry can be specialized.
        /// </summary>
        AllowSpecialization
    }
}