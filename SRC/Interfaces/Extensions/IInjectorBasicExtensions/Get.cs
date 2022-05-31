/********************************************************************************
* Get.cs                                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Interfaces
{
    public static partial class IInjectorBasicExtensions
    {
        /// <summary>
        /// Gets the service instance associated with the given interface and (optional) name.
        /// </summary>
        /// <typeparam name="TInterface">The "id" of the service to be resolved. It must be an interface.</typeparam>
        /// <param name="self">The injector itself.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <returns>The resolved service.</returns>
        /// <exception cref="ServiceNotFoundException">The service could not be found.</exception>
        public static TInterface Get<TInterface>(this IInjector self!!, string? name = null) where TInterface : class => (TInterface) self.Get(typeof(TInterface), name);

        /// <summary>
        /// Tries to get the service instance associated with the given interface and (optional) name.
        /// </summary>
        /// <typeparam name="TInterface">The "id" of the service to be resolved. It must be an interface.</typeparam>
        /// <param name="self">The injector itself.</param>
        /// <param name="name">The (optional) name of the service.</param>
        /// <returns>The requested service instance if the resolution was successful, null otherwise.</returns>
        public static TInterface? TryGet<TInterface>(this IInjector self!!, string? name = null) where TInterface : class => (TInterface?) self.TryGet(typeof(TInterface), name);
    }
}