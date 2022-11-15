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
        protected ScopedServiceEntryBase(Type @interface, string? name) : base(@interface, name)
        {
        }

        protected ScopedServiceEntryBase(Type @interface, string? name, Expression<Func<IInjector, Type, object>> factory) : base(@interface, name, factory)
        {
        }

        protected ScopedServiceEntryBase(Type @interface, string? name, Type implementation) : base(@interface, name, implementation)
        {
        }

        protected ScopedServiceEntryBase(Type @interface, string? name, Type implementation, object explicitArgs) : base(@interface, name, implementation, explicitArgs)
        {
        }

        public sealed override void Build(IDelegateCompiler? compiler, Func<int> assignSlot, params IFactoryVisitor[] visitors)
        {
            if (assignSlot is null)
                throw new ArgumentNullException(nameof(assignSlot));

            base.Build(compiler, null!, visitors);

            if (compiler is null)
                return;

            int assignedSlot = assignSlot();
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