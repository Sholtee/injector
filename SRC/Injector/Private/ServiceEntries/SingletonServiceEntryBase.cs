/********************************************************************************
* SingletonServiceEntryBase.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

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

        public override Func<IInstanceFactory, object> CreateResolver(ref int slot)
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
                {
                    if (injector.Super is not null)
                        injector = (Injector) injector.Super;

                    return injector.GetOrCreateInstance(this, relatedSlot);
                }

                if (factory.Super is not null)
                    factory = factory.Super;

                return factory.GetOrCreateInstance(this, relatedSlot);
            }
        }
    }
}