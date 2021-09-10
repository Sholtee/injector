/********************************************************************************
* IServiceIdExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    using Primitives;

    /// <summary>
    /// Defines some extensions to the <see cref="IServiceId"/> interface.
    /// </summary>
    public static class IServiceIdExtensions
    {
        /// <summary>
        /// Gets the friendly name of the <see cref="IServiceId"/> instance.
        /// </summary>
        public static string FriendlyName(this IServiceId self) 
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            string result = self.Interface.GetFriendlyName();
            if (self.Name is not null) result += $":{self.Name}";
            return result;
        }
    }
}
