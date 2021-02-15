/********************************************************************************
* TransientLifetime.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class TransientLifetime : InjectorDotNetLifetime<TransientLifetime>
    {
        public TransientLifetime() : base(bindTo: () => Transient, precedence: 10) { }

        [ModuleInitializer]
        public static void Setup() => Bind();

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, IServiceContainer owner, params Func<object, Type, object>[] customConverters)
        {
            yield return new TransientServiceEntry(iface, name, implementation, owner, customConverters);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner, params Func<object, Type, object>[] customConverters)
        {
             yield return new TransientServiceEntry(iface, name, implementation, explicitArgs, owner, customConverters);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner, params Func<object, Type, object>[] customConverters)
        {
            yield return new TransientServiceEntry(iface, name, factory, owner, customConverters);
        }
    }
}
