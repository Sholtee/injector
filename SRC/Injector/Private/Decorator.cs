/********************************************************************************
* Decorator.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using JetBrains.Annotations;

namespace Solti.Utils.Injector
{
    internal class Decorator : IDecorator
    {
        protected InjectorEntry Entry { get; }

        public Decorator([NotNull] InjectorEntry entry)
        {
            Check.NotNull(entry, nameof(entry));
            Entry = entry;
        }

        public IDecorator Decorate(Func<Type, object, object> decorator)
        {
            Check.NotNull(decorator, nameof(decorator));
            Func<IReadOnlyList<Type>, object> oldFactory = Entry.Factory;

            Entry.Factory = path => decorator(Entry.Interface, oldFactory(path));

            return this;
        }
    }

    internal class Decorator<TInterface> : Decorator, IDecorator<TInterface>
    {
        public Decorator([NotNull] InjectorEntry entry) : base(entry)
        {
        }

        public IDecorator<TInterface> Decorate(Func<TInterface, TInterface> decorator)
        {
            Check.NotNull(decorator, nameof(decorator));
            Decorate((@void, instance) => decorator((TInterface) instance));

            return this;
        }
    }
}