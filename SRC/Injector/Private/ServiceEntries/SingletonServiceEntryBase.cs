/********************************************************************************
* SingletonServiceEntryBase.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal abstract class SingletonServiceEntryBase : ProducibleServiceEntry
    {
  
        protected SingletonServiceEntryBase(Type @interface, string? name) : base(@interface, name)
        {
        }

        protected SingletonServiceEntryBase(Type @interface, string? name, Expression<Func<IInjector, Type, object>> factory) : base(@interface, name, factory)
        {
        }

        protected SingletonServiceEntryBase(Type @interface, string? name, Type implementation) : base(@interface, name, implementation)
        {
        }

        protected SingletonServiceEntryBase(Type @interface, string? name, Type implementation, object explicitArgs) : base(@interface, name, implementation, explicitArgs)
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
                {
                    if (injector.Super is not null)
                        injector = (Injector)injector.Super;

                    return injector.GetOrCreateInstance(this, assignedSlot);
                }

                if (factory.Super is not null)
                    factory = factory.Super;

                return factory.GetOrCreateInstance(this, assignedSlot);
            };
        }
    }
}