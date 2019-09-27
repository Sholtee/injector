/********************************************************************************
* QueryMode.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    [Flags]
    public enum QueryMode
    {
        /// <summary>
        /// Allows returning NULL if the desired service was not found.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Instructs the <see cref="IServiceContainer"/> to throw if the desired service could not be found.
        /// </summary>
        ThrowOnError,

        /// <summary>
        /// Allows "auto registering" if the entry can be specialized.
        /// </summary>
        AllowSpecialization
    }
}