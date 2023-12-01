/********************************************************************************
* ScopedServiceEntryBase.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal abstract class ScopedServiceEntryBase: ProducibleServiceEntry
    {
        protected ScopedServiceEntryBase(Type @interface, object? name, Expression<FactoryDelegate> factory, ServiceOptions options) : base(@interface, name, factory, options)
        {
        }

        protected ScopedServiceEntryBase(Type @interface, object? name, Type implementation, ServiceOptions options) : base(@interface, name, implementation, options)
        {
        }

        protected ScopedServiceEntryBase(Type @interface, object? name, Type implementation, object explicitArgs, ServiceOptions options) : base(@interface, name, implementation, explicitArgs, options)
        {
        }

        public sealed override void Build(IBuildContext context, IReadOnlyList<IFactoryVisitor> visitors)
        {
            base.Build(context, visitors);
            AssignedSlot = context.AssignSlot();
        }
    }
}