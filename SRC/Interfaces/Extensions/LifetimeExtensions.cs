/********************************************************************************
* LifetimeExtensions.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Defines several handy extensions against the <see cref="Lifetime"/> class.
    /// </summary>
    public static class LifetimeExtensions
    {
        /// <summary>
        /// Sets the capacity if the operation is supported. 
        /// </summary>
        /// <remarks>This extensions is intended to be used to configure <see cref="Lifetime.Pooled"/> lifetime.</remarks>
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
