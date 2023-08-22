/********************************************************************************
* IServiceActivator.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes the contract how to create servive instances.
    /// </summary>
    public interface IServiceActivator: IInjector
    {
        /// <summary>
        /// The parent factory. NULL in case of root factory.
        /// </summary>
        IServiceActivator? Super { get; }

        /// <summary>
        /// Gets or creates a service instance.
        /// </summary>
        object GetOrCreateInstance(AbstractServiceEntry service);
    }
}
