/********************************************************************************
* DefaultDependencyResolvers.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Immutable;

namespace Solti.Utils.DI
{
    using Interfaces;
    using Internals;

    /// <summary>
    /// The default <see cref="IDependencyResolver"/> implementations in a proper order.
    /// </summary>
    /// <remarks>This value is used when the <see cref="ServiceOptions.DependencyResolvers"/> property is null</remarks>
    public static class DefaultDependencyResolvers
    {
        /// <summary>
        /// The actual list.
        /// </summary>
        public static ImmutableList<IDependencyResolver> Value { get; set; } = ImmutableList.Create<IDependencyResolver>
        (
            new ExplicitArgResolver_Dict(),
            new ExplicitArgResolver_Obj(),
            new RegularLazyDependencyResolver(),
            new LazyDependencyResolver(),
            new RegularDependencyResolver()
        );
    }
}
