/********************************************************************************
* IServiceCollection.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    [Flags]
    internal enum QueryMode
    {
        /// <summary>
        /// Allows returning NULL if the desired service was not found.
        /// </summary>
        Default = 0,

        ThrowOnError,

        /// <summary>
        /// Allows returning the open generic pair of a missing entry (if available).
        /// </summary>
        AllowSpecialization
    }

    internal interface IServiceCollection: ICollection<AbstractServiceEntry>
    {
        AbstractServiceEntry Get(Type iface, QueryMode mode = QueryMode.Default);
    }
}
