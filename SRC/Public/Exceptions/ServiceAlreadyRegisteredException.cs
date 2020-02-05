/********************************************************************************
* ServiceAlreadyRegisteredException.cs                                          *
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
    /// The exception that is thrown when a service has already been registered.
    /// </summary>
    [SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Key must be passed.")]
    public sealed class ServiceAlreadyRegisteredException: Exception
    {
        /// <summary>
        /// Creates a new <see cref="ServiceAlreadyRegisteredException"/> instance.
        /// </summary>
        /// <param name="key">The "id" of the service.</param>
        /// <param name="innerException">The inner exception (if it is present).</param>
        internal ServiceAlreadyRegisteredException(IServiceID key, Exception innerException = null): base(string.Format(Resources.Culture, Resources.ALREADY_REGISTERED, key.FriendlyName()), innerException)
        {
        }
    }
}
