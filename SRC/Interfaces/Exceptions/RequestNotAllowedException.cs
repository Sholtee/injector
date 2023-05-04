/********************************************************************************
* RequestNotAllowedException.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// The exception that is thrown when a service request is not allowed.
    /// </summary>
    /// <remarks>This is a general error that is thrown in different scenarios (for instance in case of StrictDI violation).</remarks>
    public sealed class RequestNotAllowedException: Exception
    {
        /// <summary>
        /// Creates a new <see cref="RequestNotAllowedException"/> instance.
        /// </summary>
        public RequestNotAllowedException(string message) : this(message, null!)
        {
        }

        /// <summary>
        /// Creates a new <see cref="RequestNotAllowedException"/> instance.
        /// </summary>
        public RequestNotAllowedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
