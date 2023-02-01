/********************************************************************************
* DefaultDependencyResolvers.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
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
        public static IReadOnlyList<IDependencyResolver> Value { get; } = ImmutableArray.Create<IDependencyResolver>
        (
            ExplicitArgResolver_Dict.Instance,
            ExplicitArgResolver_Obj.Instance,
            LazyDependencyResolver.Instance,
            RegularDependencyResolver.Instance
        );
    }
}
