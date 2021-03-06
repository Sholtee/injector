﻿/********************************************************************************
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
    public class RequestNotAllowedException: Exception
    {
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
