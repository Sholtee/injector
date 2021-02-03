/********************************************************************************
* LifetimeExtensions.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Defines several handy extensions for the <see cref="Lifetime"/> class.
    /// </summary>
    public static class LifetimeExtensions
    {
        /// <summary>
        /// Sets the capacity. 
        /// </summary>
        public static Lifetime WithCapacity(this Lifetime self, int capacity)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            self = (Lifetime) self.Clone();

            if (self is not IHasCapacity hasCapacity)
                throw new NotSupportedException();

            hasCapacity.Capacity = capacity;
            return self;
        }
    }
}
