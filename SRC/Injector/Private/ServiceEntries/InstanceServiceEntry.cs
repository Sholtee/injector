/********************************************************************************
* InstanceServiceEntry.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class InstanceServiceEntry : AbstractServiceEntry
    {
        private readonly object FInstance;

        public InstanceServiceEntry(Type @interface, string? name, object instance, IServiceRegistry? owner) : base(@interface, name, null, owner)
        {
            Ensure.Parameter.IsNotNull(instance, nameof(instance));

            FInstance = instance;

            //
            // Ez kivetelt fog dobni ha "@interface"-en akar csak egy aspektus is van (peldanynak nincs
            // factory-ja -> nem lehet proxy-zni).
            //

            this.ApplyAspects();

            //
            // SetInstance() ne legyen hivva.
            //

            State = ServiceEntryStates.Built;
        }

        public override Lifetime? Lifetime { get; } = Lifetime.Instance;

        public override AbstractServiceEntry WithOwner(IServiceRegistry owner) => this;

        public override object CreateInstance(IInjector scope) => throw new InvalidOperationException();

        public override object GetSingleInstance() => FInstance;

        public override bool IsShared { get; } = true;
    }
}