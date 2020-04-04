/********************************************************************************
* CreateChild.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Internals;

    public static partial class IServiceContainerExtensions
    {
        /// <summary>
        /// Creates a new child container.
        /// </summary>
        /// <returns>The newly created child container.</returns>
        /// <remarks>
        /// <list type="bullet">
        /// <item>
        /// <description>The container created by this method inherits the state of its parent.</description>
        /// </item>
        /// <item>
        /// <description>Lifetime of the created child is managed by its parent.</description>
        /// </item>
        /// </list>
        /// </remarks>
        /// <exception cref="InvalidOperationException">The count of <see cref="IComposite{IServiceContainer}.Children"/> reached the limit that was set in the <see cref="Config"/>.</exception>
        public static IServiceContainer CreateChild(this IServiceContainer self)
        {
            Ensure.Parameter.IsNotNull(self, nameof(self));

            return new ServiceContainer(self);
        }
    }
}