/********************************************************************************
* Remove.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Linq;

namespace Solti.Utils.DI.Interfaces
{
    using Properties;

    public static partial class IServiceCollectionBasicExtensions
    {
        /// <summary>
        /// Removes the <see cref="AbstractServiceEntry"/> associated with the given <paramref name="iface"/> and (optional) <paramref name="name"/>.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="iface">The service interface.</param>
        /// <param name="name">The (optional) service name.</param>
        public static IServiceCollection Remove(this IServiceCollection self, Type iface, string? name)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (iface is null)
                throw new ArgumentNullException(nameof(iface));

            AbstractServiceEntry entry = self.SingleOrDefault
            (
                svc => svc.Interface == iface && svc.Name == name
            ) ?? throw new ServiceNotFoundException
            (
                string.Format
                (
                    Resources.Culture,
                    Resources.SERVICE_NOT_FOUND,
                    new MissingServiceEntry(iface, name).ToString(shortForm: true)
                )
            );
            
            bool removed = self.Remove(entry);
            Debug.Assert(removed, "Entry cannot be removed");

            return self;
        }

        /// <summary>
        /// Removes the <see cref="AbstractServiceEntry"/> associated with the given <paramref name="iface"/>.
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="iface">The service interface.</param>
        public static IServiceCollection Remove(this IServiceCollection self, Type iface) => self.Remove(iface, null);

        /// <summary>
        /// Removes the <see cref="AbstractServiceEntry"/> associated with the given <typeparamref name="TInterface"/> and (optional) <paramref name="name"/>.
        /// </summary>
        /// <typeparam name="TInterface">The service interface.</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The (optional) service name.</param>
        public static IServiceCollection Remove<TInterface>(this IServiceCollection self, string? name) where TInterface : class => self.Remove(typeof(TInterface), name);

        /// <summary>
        /// Removes the <see cref="AbstractServiceEntry"/> associated with the given <typeparamref name="TInterface"/>.
        /// </summary>
        /// <typeparam name="TInterface">The service interface.</typeparam>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        public static IServiceCollection Remove<TInterface>(this IServiceCollection self) where TInterface : class => self.Remove(typeof(TInterface));
    }
}