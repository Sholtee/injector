/********************************************************************************
* ParameterValidatorAspect.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Extensions.Aspects
{
    using Interfaces;

    /// <summary>
    /// Defines an aspect that validates method parameters.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public sealed class ParameterValidatorAspect : AspectAttribute
    {
        /// <summary>
        /// See <see cref="AspectAttribute.GetInterceptor(Type)"/>.
        /// </summary>
        public override Type GetInterceptor(Type iface) => typeof(ParameterValidator<>).MakeGenericType(iface ?? throw new ArgumentNullException(nameof(iface)));
    }

}
