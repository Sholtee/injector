/********************************************************************************
* ServiceCollection.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI
{
    using Interfaces;

    /// <summary>
    /// Exposes the underlying <see cref="IServiceCollection"/> implementation.
    /// </summary>
    public static class ServiceCollection
    {
        /// <summary>
        /// Creates a new <see cref="IServiceCollection"/> instance.
        /// </summary>
        public static IServiceCollection Create(ServiceOptions? serviceOptions = null) => new Internals.ServiceCollection(serviceOptions);
    }
}
