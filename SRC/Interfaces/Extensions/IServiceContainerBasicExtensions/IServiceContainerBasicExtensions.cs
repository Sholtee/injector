/********************************************************************************
* IServiceContainerBasicExtensions.cs                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Defines basic extensions for the <see cref="IServiceContainer"/> interface.
    /// </summary>
    public static partial class IServiceContainerBasicExtensions
    {
        private static IServiceContainer Add(this IServiceContainer src, IEnumerable<AbstractServiceEntry> entries)
        {
            foreach (AbstractServiceEntry entry in entries)
            {
                src.Add(entry);
            }
            return src;
        }
    }
}