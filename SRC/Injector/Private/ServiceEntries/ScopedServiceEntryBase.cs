/********************************************************************************
* ScopedServiceEntryBase.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal abstract class ScopedServiceEntryBase: ProducibleServiceEntry
    {
        protected ScopedServiceEntryBase(Type @interface, string? name, Expression<FactoryDelegate> factory, ServiceOptions options) : base(@interface, name, factory, options)
        {
        }

        protected ScopedServiceEntryBase(Type @interface, string? name, Type implementation, ServiceOptions options) : base(@interface, name, implementation, options)
        {
        }

        protected ScopedServiceEntryBase(Type @interface, string? name, Type implementation, object explicitArgs, ServiceOptions options) : base(@interface, name, implementation, explicitArgs, options)
        {
        }

        public sealed override void Build(IBuildContext context, params IFactoryVisitor[] visitors)
        {
            base.Build(context, visitors);

            int assignedSlot = context.AssignSlot();

            ResolveInstance = (IInstanceFactory factory) => factory.GetOrCreateInstance(this, assignedSlot);
        }
    }
}