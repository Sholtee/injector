/********************************************************************************
* ContextualServiceEntry.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class ContextualServiceEntry : AbstractServiceEntry
    {
        private object? FInstance;

        private ContextualServiceEntry(ContextualServiceEntry original, IServiceRegistry owner) : base(original.Interface, original.Name, null, Ensure.Parameter.IsNotNull(owner, nameof(owner)))
        {
            Selector = original.Selector;
            Flags = ServiceEntryFlags.Built | ServiceEntryFlags.CreateSingleInstance | ServiceEntryFlags.Validated;
        }

        public ContextualServiceEntry(Type @interface, string? name, Func<IServiceRegistry, object> selector) : base(@interface, name, null, null) =>
            Selector = selector;

        public override object CreateInstance(IInjector scope) => throw new InvalidOperationException();

        public override object CreateInstance(IInjector scope, out IDisposable? lifetime)
        {
            if (Owner is null)
                throw new InvalidOperationException();

            lifetime = null;
            return FInstance ??= Selector(Owner);
        }

        public override object GetSingleInstance()
        {
            if (Owner is null)
                throw new InvalidOperationException();

            return FInstance ??= Selector(Owner);
        }

        public Func<IServiceRegistry, object> Selector { get; }

        public override AbstractServiceEntry CopyTo(IServiceRegistry owner) => new ContextualServiceEntry(this, Ensure.Parameter.IsNotNull(owner, nameof(owner)));
    }
}