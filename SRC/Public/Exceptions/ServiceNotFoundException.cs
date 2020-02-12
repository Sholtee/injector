/********************************************************************************
* ServiceNotFoundException.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI
{
    using Internals;
    using Properties;

    /// <summary>
    /// The exception that is thrown when a service could not be found.
    /// </summary>
    [SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Key must be passed.")]
    public sealed class ServiceNotFoundException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="ServiceNotFoundException"/> instance.
        /// </summary>
        /// <param name="key">The "id" of the service that could not be found.</param>
        internal ServiceNotFoundException(IServiceId key) : base(string.Format(Resources.Culture, Resources.SERVICE_NOT_FOUND, key.FriendlyName()))
        {
        }
    }
}
