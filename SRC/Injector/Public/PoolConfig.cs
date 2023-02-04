/********************************************************************************
* PoolConfig.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    /// <summary>
    /// Contains pool realted configurations.
    /// </summary>
    public sealed record PoolConfig
    {
        /// <summary>
        /// The maximum capacity of the pool.
        /// </summary>
        public int Capacity { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// Specifies whether the pool should block or throw an exception if there is no item to return.
        /// </summary>
        public bool Blocking { get; set; } = true;

        /// <summary>
        /// The default value.
        /// </summary>
        public static PoolConfig Default { get; } = new();
    }
}
