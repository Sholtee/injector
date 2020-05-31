/********************************************************************************
* TransactionalAspect.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Extensions.Aspects
{
    using Interfaces;

    /// <summary>
    /// Defines an aspect that manages database transactions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public sealed class TransactionalAspect : AspectAttribute
    {
        /// <summary>
        /// See <see cref="AspectAttribute.GetInterceptor(Type)"/>.
        /// </summary>
        public override Type GetInterceptor(Type iface) => typeof(TransactionManager<>).MakeGenericType(iface ?? throw new ArgumentNullException(nameof(iface)));
    }
}
