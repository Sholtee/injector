/********************************************************************************
* Decorate.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Interfaces
{
    public static partial class IServiceCollectionBasicExtensions
    {
        /// <summary>
        /// Hooks into the instantiating process of last registered service. Useful when you want to add additional functionality (e.g. parameter validation).
        /// </summary>
        /// <param name="self">The target <see cref="IServiceCollection"/>.</param>
        /// <param name="decorator">The decorator funtion. It must return the decorated instance. The original instance can be accessed via the 3rd parameter of the decorator function.</param>
        /// <remarks>You can't create proxies against instances and open generic services. A service can be decorated multiple times.</remarks>
        /// <exception cref="InvalidOperationException">When proxying not allowed (see above).</exception>
        public static IServiceCollection Decorate(this IServiceCollection self, Expression<DecoratorDelegate> decorator)
        {
            if (self is null)
                throw new ArgumentNullException(nameof(self));

            if (decorator is null)
                throw new ArgumentNullException(nameof(decorator));

            self.Last().Decorate(decorator);
            return self;
        }
    }
}