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
        protected ScopedServiceEntryBase(Type type, object? key, Expression<FactoryDelegate> factory, ServiceOptions options) : base(type, key, factory, options)
        {
        }

        protected ScopedServiceEntryBase(Type type, object? key, Type implementation, ServiceOptions options) : base(type, key, implementation, options)
        {
        }

        protected ScopedServiceEntryBase(Type type, object? key, Type implementation, object explicitArgs, ServiceOptions options) : base(type, key, implementation, explicitArgs, options)
        {
        }

        public sealed override void Build(IBuildContext context, IReadOnlyList<IFactoryVisitor> visitors)
        {
            base.Build(context, visitors);
            AssignedSlot = context.AssignSlot();
        }
    }
}