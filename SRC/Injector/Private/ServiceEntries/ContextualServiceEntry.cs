﻿/********************************************************************************
* ContextualServiceEntry.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal class ContextualServiceEntry : AbstractServiceEntry
    {
        private readonly object? FInstance;

        private readonly Func<IServiceRegistry, object> FSelector;

        private ContextualServiceEntry(ContextualServiceEntry original, IServiceRegistry owner) : base(original.Interface, original.Name)
        {
            Owner = Ensure.Parameter.IsNotNull(owner, nameof(owner));
            FSelector = original.FSelector;
            FInstance = FSelector(owner);
            State = ServiceEntryStates.Built;
        }

        public ContextualServiceEntry(Type @interface, string? name, Func<IServiceRegistry, object> selector) : base(@interface, name, null, null)
        {
            FSelector = Ensure.Parameter.IsNotNull(selector, nameof(selector));
        }

        public override object CreateInstance(IInjector scope) => throw new InvalidOperationException();

        public override object GetSingleInstance() => FInstance ?? throw new InvalidOperationException();

        public override IServiceRegistry? Owner { get; }

        public override AbstractServiceEntry CopyTo(IServiceRegistry owner) => new ContextualServiceEntry(this, Ensure.Parameter.IsNotNull(owner, nameof(owner)));
    }
}