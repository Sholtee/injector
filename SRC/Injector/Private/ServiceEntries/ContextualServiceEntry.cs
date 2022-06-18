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
        public new Func<IInjector, Type, object> Factory { get; }

        public ContextualServiceEntry(Type @interface, string? name, Func<IInjector, Type, object> factory) : base(@interface, name)
        {
            Factory = factory;
            State = ServiceEntryStateFlags.Built | ServiceEntryStateFlags.Validated;
        }

        public override object CreateInstance(IInjector scope, out object? lifetime)
        {
            lifetime = null;
            return Factory(scope, Interface);
        }

        public override ServiceEntryFlags Features { get; } = ServiceEntryFlags.CreateSingleInstance;
    }
}