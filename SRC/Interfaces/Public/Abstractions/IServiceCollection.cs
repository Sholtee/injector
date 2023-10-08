/********************************************************************************
* IServiceCollection.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Specifies the contract of service entry sets.
    /// </summary>
    /// <remarks>Only one entry can be registered with a particular <see cref="AbstractServiceEntry.Interface"/> and <see cref="AbstractServiceEntry.Name"/> pair.</remarks>
    public interface IServiceCollection : ICollection<AbstractServiceEntry>
    {
        /// <summary>
        /// Determines if the collection contains entry with the specific <paramref name="id"/>
        /// </summary>
        bool Contains(IServiceId id);

        /// <summary>
        /// Removes the entry associated with the given <paramref name="id"/>.
        /// </summary>
        bool Remove(IServiceId id);

        /// <summary>
        /// Tries to find the entry associated with the given <paramref name="id"/>.
        /// </summary>
        AbstractServiceEntry? TryFind(IServiceId id);

        /// <summary>
        /// Makes this collection read only.
        /// </summary>
        public void MakeReadOnly();
    }
}