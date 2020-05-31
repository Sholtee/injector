/********************************************************************************
* TransactionalAttribute.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Data;

namespace Solti.Utils.DI.Extensions.Aspects
{
    /// <summary>
    /// Marks a method to be transacted.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class TransactionalAttribute : Attribute 
    {
        /// <summary>
        /// The <see cref="IsolationLevel"/> of the transaction.
        /// </summary>
        public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.Unspecified;
    }
}
