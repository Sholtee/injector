﻿/********************************************************************************
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

    internal sealed class TransientLifetime : Lifetime
    {
        [ModuleInitializer]
        public static void Setup() => Transient = new TransientLifetime();

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

        public override bool IsCompatible(AbstractServiceEntry entry) => entry is TransientServiceEntry;

        public override string ToString() => nameof(Transient);
    }
}
