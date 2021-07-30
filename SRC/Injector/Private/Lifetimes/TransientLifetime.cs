/********************************************************************************
* TransientLifetime.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class TransientLifetime : InjectorDotNetLifetime, IConcreteLifetime<TransientLifetime>
    {
        public TransientLifetime() : base(bindTo: () => Transient, precedence: 10) { }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, IServiceContainer owner)
        {
            yield return new TransientServiceEntry(iface, name, implementation, owner, Config.Value.Injector.MaxSpawnedTransientServices);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner)
        {
             yield return new TransientServiceEntry(iface, name, implementation, explicitArgs, owner, Config.Value.Injector.MaxSpawnedTransientServices);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner)
        {
            yield return new TransientServiceEntry(iface, name, factory, owner, Config.Value.Injector.MaxSpawnedTransientServices);
        }
    }
}
