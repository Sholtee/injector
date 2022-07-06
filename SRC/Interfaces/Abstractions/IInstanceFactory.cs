/********************************************************************************
* IInstanceFactory.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes the methods how to create servive instances.
    /// </summary>
    public interface IInstanceFactory
    {
        /// <summary>
        /// The parent factory. NULL in case of root factory.
        /// </summary>
        IInstanceFactory? Super { get; }

        /// <summary>
        /// Creates a new service instance from the given <see cref="AbstractServiceEntry"/>.
        /// </summary>
        object CreateInstance(AbstractServiceEntry requested);

        /// <summary>
        /// Gets or creates a service instance assigned to the given slot.
        /// </summary>
        object GetOrCreateInstance(AbstractServiceEntry requested, int slot);
    }
}
