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
    public interface IInstanceFactory: IInjector
    {
        /// <summary>
        /// The parent factory. NULL in case of root factory.
        /// </summary>
        IInstanceFactory? Super { get; }

        /// <summary>
        /// Gets or creates a service instance assigned to the given slot.
        /// </summary>
        /// <remarks>Setting the <paramref name="slot"/> to <see cref="Consts.CREATE_ALWAYS"/> will instruct the system to instantiate a new instance on each request.</remarks>
        object GetOrCreateInstance(AbstractServiceEntry requested, int slot);
    }
}
