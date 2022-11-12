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
        private int? FAssignedSlot;

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

        public sealed override void Build(IDelegateCompiler? compiler, ref int slots, params IFactoryVisitor[] visitors)
        {
            base.Build(compiler, ref slots, visitors);

            if (compiler is not null)
                FAssignedSlot = slots++;
        }

        public sealed override int? AssignedSlot => FAssignedSlot;

        public sealed override object Resolve(IInstanceFactory factory)
        {
            if (FAssignedSlot is null)
                throw new InvalidOperationException();

            //
            // Inlining works against non-interface, non-virtual methods only
            //

            if (factory is Injector injector)
                return injector.GetOrCreateInstance(this, FAssignedSlot);

            return factory.GetOrCreateInstance(this, FAssignedSlot);
        }
    }
}