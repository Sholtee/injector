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
        protected ScopedServiceEntryBase(Type @interface, string? name, Expression<FactoryDelegate> factory, bool supportAspects) : base(@interface, name, factory, supportAspects)
        {
        }

        protected ScopedServiceEntryBase(Type @interface, string? name, Type implementation, bool supportAspects) : base(@interface, name, implementation, supportAspects)
        {
        }

        protected ScopedServiceEntryBase(Type @interface, string? name, Type implementation, object explicitArgs, bool supportAspects) : base(@interface, name, implementation, explicitArgs, supportAspects)
        {
        }

        public sealed override void Build(IBuildContext? context, params IFactoryVisitor[] visitors)
        {
            base.Build(context, visitors);

            if (context is null)
                return;

            int assignedSlot = context.AssignSlot();

            ResolveInstance = (IInstanceFactory factory) =>
            {
                //
                // Inlining works against non-interface, non-virtual methods only
                //

                if (factory is Injector injector)
                    return injector.GetOrCreateInstance(this, assignedSlot);

                return factory.GetOrCreateInstance(this, assignedSlot);
            };
        }
    }
}