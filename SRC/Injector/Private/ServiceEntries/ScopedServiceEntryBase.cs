/********************************************************************************
* ScopedServiceEntryBase.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

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

        public override ServiceResolver CreateResolver(ref int slot)
        {
            int relatedSlot = slot++;

            return Resolve;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            object Resolve(IInstanceFactory factory)
            {
                //
                // Inlining works against non-interface, non-virtual methods only
                //

                if (factory is Injector injector)
                    return injector.GetOrCreateInstance(this, relatedSlot);

                return factory.GetOrCreateInstance(this, relatedSlot);
            }
        }
    }
}