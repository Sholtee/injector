/********************************************************************************
* ParameterValidatorAspect.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Extensions.Aspects
{
    using Interfaces;
    using Proxy;

    /// <summary>
    /// Defines an aspect that validates method parameters.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public sealed class ParameterValidatorAspect : AspectAttribute
    {
        /// <summary>
        /// Specifies that the validation should return all the validation errors.
        /// </summary>
        public bool Aggregate { get; }

        /// <summary>
        /// Creates a new <see cref="ParameterValidatorAspect"/> instance.
        /// </summary>
        public ParameterValidatorAspect(bool aggregate = false)
        {
            Kind = AspectKind.Factory;
            Aggregate = aggregate;
        }

        /// <summary>
        /// See <see cref="AspectAttribute.GetInterceptor(Type)"/>.
        /// </summary>
        public override object GetInterceptor(IInjector injector, Type iface, object instance) => ProxyFactory.Create
        (
            iface ?? throw new ArgumentNullException(nameof(iface)),
            typeof(ParameterValidator<>).MakeGenericType(iface),
            new[]
            {
                iface, // target
                typeof(bool)  // aggregate
            },
            instance ?? throw new ArgumentNullException(nameof(instance)),
            Aggregate
        );
    }
}
