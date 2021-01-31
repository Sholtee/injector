/********************************************************************************
* ServiceEntryExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Defines several handy extensions for the <see cref="AbstractServiceEntry"/> class.
    /// </summary>
    public static class ServiceEntryExtensions
    {
        /// <summary>
        /// Sets the capacity of the given entry. 
        /// </summary>
        public static AbstractServiceEntry WithCapacity(this AbstractServiceEntry self, int capacity)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (self is not IHasCapacity hasCapacity)
                throw new NotSupportedException();

            hasCapacity.Capacity = capacity;
            return self;
        }
    }
}
