/********************************************************************************
* SingletonLifetime.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class SingletonLifetime : InjectorDotNetLifetime<SingletonLifetime>
    {
        public SingletonLifetime() : base(bindTo: () => Singleton, precedence: 30) { }

        [ModuleInitializer]
        public static void Setup() => Bind();

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, IServiceContainer owner)
        {
            yield return new SingletonServiceEntry(iface, name, implementation, owner);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Type implementation, IReadOnlyDictionary<string, object?> explicitArgs, IServiceContainer owner)
        {
            yield return new SingletonServiceEntry(iface, name, implementation, explicitArgs, owner);
        }

        public override IEnumerable<AbstractServiceEntry> CreateFrom(Type iface, string? name, Func<IInjector, Type, object> factory, IServiceContainer owner)
        {
            yield return new SingletonServiceEntry(iface, name, factory, owner);
        }
    }
}
