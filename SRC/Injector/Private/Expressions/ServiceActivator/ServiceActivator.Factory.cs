/********************************************************************************
* ServiceActivator.Factory.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal static partial class ServiceActivator
    {
        public static Expression<FactoryDelegate> ResolveFactory(ConstructorInfo constructor, object? userData, IReadOnlyList<IDependencyResolver>? resolvers) =>
            CreateActivator<FactoryDelegate>(constructor, userData, resolvers ?? DefaultDependencyResolvers.Value);
    }
}