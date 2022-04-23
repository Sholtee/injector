/********************************************************************************
* ContextualServiceEntry.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class ContextualServiceEntry : ProducibleServiceEntry
    {
        public ContextualServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory) : base(@interface, name, factory) =>
            Flags = ServiceEntryFlags.CreateSingleInstance | ServiceEntryFlags.Validated;

        public override object CreateInstance(IInjector scope, out object? lifetime)
        {
            lifetime = null;
            return Factory!(scope, Interface);
        }
    }
}