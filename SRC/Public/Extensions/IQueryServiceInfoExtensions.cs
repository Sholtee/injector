/********************************************************************************
* IQueryServiceInfoExtensions.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI
{
    /// <summary>
    /// Defines several handy extensions for the <see cref="IQueryServiceInfo"/> interface.
    /// </summary>
    public static class IQueryServiceInfoExtensions
    {
        /// <summary>
        /// Gets basic informations about a registered service.
        /// </summary>
        /// <typeparam name="TInterface">The "id" of the service to be queried. It must be an interface.</typeparam>
        /// <param name="self">The object implementing the <see cref="IQueryServiceInfo"/> interface.</param>
        /// <returns>An <see cref="IServiceInfo"/> instance.</returns>
        public static IServiceInfo QueryServiceInfo<TInterface>(this IQueryServiceInfo self) => self.QueryServiceInfo(typeof(TInterface));
    }
}