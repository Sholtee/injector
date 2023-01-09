/********************************************************************************
* IServiceCollection.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Specifies the contract for an <see cref="AbstractServiceEntry"/> set.
    /// </summary>
    /// <remarks>Only one entry can be registered with a given <see cref="AbstractServiceEntry.Interface"/> and <see cref="AbstractServiceEntry.Name"/> pair.</remarks>
    public interface IServiceCollection : ICollection<AbstractServiceEntry>
    {
        /// <summary>
        /// Defines the general service behavior.
        /// </summary>
        /// <remarks>You can override this setting by calling the Add method directly.</remarks>
        ServiceOptions ServiceOptions { get; }
    }
}