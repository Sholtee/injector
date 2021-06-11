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
        public static string FriendlyName(this IServiceId src) 
        {
            if (src is null)
                throw new ArgumentNullException(nameof(src));

            string result = src.Interface.GetFriendlyName();
            if (src.Name is not null) result += $":{src.Name}";
            return result;
        }
    }
}
