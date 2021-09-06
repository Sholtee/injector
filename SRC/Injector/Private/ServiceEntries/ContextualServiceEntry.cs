/********************************************************************************
* ContextualServiceEntry.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class ContextualServiceEntry : AbstractServiceEntry
    {
        private readonly IReadOnlyCollection<ServiceReference> FInstances;

        private readonly Func<IServiceRegistry, object> FSelector;

        public ContextualServiceEntry(Type @interface, string? name, Func<IServiceRegistry, object> selector) : base(@interface, name, null, null)
        {
            FInstances = Array.Empty<ServiceReference>();
            FSelector = Ensure.Parameter.IsNotNull(selector, nameof(selector));
        }

        private ContextualServiceEntry(ContextualServiceEntry original, IServiceRegistry owner) : base(original.Interface, original.Name)
        {
            Owner = Ensure.Parameter.IsNotNull(owner, nameof(owner));
            FSelector = original.FSelector;
            FInstances = new[] 
            {
                new ServiceReference(this, FSelector(owner), externallyOwned: true)
            };
            State = ServiceEntryStates.Built;
        }

        public override IServiceRegistry? Owner { get; }

        public override AbstractServiceEntry CopyTo(IServiceRegistry owner) => new ContextualServiceEntry(this, Ensure.Parameter.IsNotNull(owner, nameof(owner)));

        public override bool SetInstance(IServiceReference serviceReference) => throw new NotImplementedException();

        public override IReadOnlyCollection<IServiceReference> Instances => FInstances;
    }
}