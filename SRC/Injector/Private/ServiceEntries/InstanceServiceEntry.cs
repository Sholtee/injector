/********************************************************************************
* InstanceServiceEntry.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Interfaces.Properties;

    internal sealed class InstanceServiceEntry : SingletonServiceEntry
    {
        public InstanceServiceEntry(Type iface, object? name, object instance, ServiceOptions options) : base
        (
            iface,
            name,
            (_, _) => instance,
            options with { DisposalMode = ServiceDisposalMode.Suppress }
        )
        {
            if (instance is null)
                throw new ArgumentNullException(nameof(instance));

            if (iface.IsGenericTypeDefinition)
                throw new ArgumentException(Resources.OPEN_GENERIC, nameof(iface));
        }

        public override AbstractServiceEntry Specialize(params Type[] genericArguments)
            => throw new NotSupportedException();

        public override void Decorate(Expression<DecoratorDelegate> applyProxy)
            => throw new NotSupportedException(Resources.DECORATING_NOT_SUPPORTED);

        public override LifetimeBase? Lifetime { get; } = DI.Lifetime.Instance;
    }
}