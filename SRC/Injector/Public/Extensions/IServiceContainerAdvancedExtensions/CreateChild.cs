/********************************************************************************
* CreateChild.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Interfaces;
    using Internals;
    using Primitives.Patterns;

    public static partial class IServiceContainerAdvancedExtensions
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
        public static IServiceContainer CreateChild(this IServiceContainer self) => new ServiceContainer
        (
            Ensure.Parameter.IsNotNull(self, nameof(self))
        );
    }
}