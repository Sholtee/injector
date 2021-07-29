/********************************************************************************
* IInjectorExtensions.Validation.cs                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Diagnostics
{
    using Interfaces;
    using Primitives;
    using Proxy;

    public static partial class IInjectorExtensions
    {
        internal static object? GetUnderlyingInstance(object instance, Type iface)
        {
            Type interceptor = typeof(InterfaceInterceptor<>).MakeGenericType(iface);

            Func<object, object?> getter = interceptor
                .GetProperty(nameof(InterfaceInterceptor<object>.Target))
                .ToGetter(); // gyorsitotarazot

            for (; instance is not null && interceptor.IsInstanceOfType(instance);)
            {
                instance = getter(instance)!;
            }

            return instance;
        }

        /// <summary>
        /// Validates the service registrations.
        /// </summary>
        public static void Validate(this IInjector injector)
        {
            if (injector is null)
                throw new ArgumentNullException(nameof(injector));

            foreach (AbstractServiceEntry entry in injector.UnderlyingContainer)
            {
                //
                // Generikus, Instance stb nem jatszanak
                //

                if (entry.Factory is null)
                    continue;

                object instance = injector.Get(entry.Interface, entry.Name);

                IHasValidationMethod? validator = instance as IHasValidationMethod ?? GetUnderlyingInstance(instance, entry.Interface) as IHasValidationMethod;
                validator?.Validate();
            }
        }
    }
}
