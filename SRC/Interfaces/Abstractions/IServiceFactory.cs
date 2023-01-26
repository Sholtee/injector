/********************************************************************************
* IServiceFactory.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes the contract how to create servive instances.
    /// </summary>
    public interface IServiceFactory: IInjector
    {
        /// <summary>
        /// Contains some constants related to the <see cref="IServiceFactory"/> interface.
        /// </summary>
        public static class Consts
        {
            /// <summary>
            /// Invalid slot..
            /// </summary>
            public const int INVALID_SLOT = CREATE_ALWAYS;

            /// <summary>
            /// The service requires to be created each time it is requested.
            /// </summary>
            public const int CREATE_ALWAYS = -1;
        }

        /// <summary>
        /// The parent factory. NULL in case of root factory.
        /// </summary>
        IServiceFactory? Super { get; } // TODO: REMOVE

        /// <summary>
        /// Gets or creates a service instance.
        /// </summary>
        object GetOrCreateInstance(AbstractServiceEntry requested);
    }
}
