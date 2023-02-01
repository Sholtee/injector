/********************************************************************************
* FactoryResolver.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class FactoryResolver: FactoryResolverBase
    {
        public Expression<FactoryDelegate> Resolve(ConstructorInfo constructor, object? userData) => CreateActivator<FactoryDelegate>
        (
            constructor,
            userData
        );

        public Expression<FactoryDelegate> Resolve(Type type, object? userData)
        {
            EnsureCanBeInstantiated(type);
            return Resolve(type.GetApplicableConstructor(), userData);
        }

        public FactoryResolver(IReadOnlyList<IDependencyResolver>? additionalResolvers): base(additionalResolvers) { }
    }
}