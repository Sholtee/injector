/********************************************************************************
* RequestNotAllowedException.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Internals;

    /// <summary>
    /// The exception that is thrown when a service request is not allowed.
    /// </summary>
    public class RequestNotAllowedException: Exception
    {
        internal RequestNotAllowedException(IServiceId requestor, IServiceId requested, string reason) : this(reason) 
        {
            Data.Add(nameof(requestor), requestor.FriendlyName());
            Data.Add(nameof(requested), requested.FriendlyName());
        }

        /// <summary>
        /// Creates a new <see cref="RequestNotAllowedException"/> instance.
        /// </summary>
        public RequestNotAllowedException()
        {
        }

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
