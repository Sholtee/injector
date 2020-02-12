/********************************************************************************
* RequestNotAllowedException.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI
{
    using Internals;

    /// <summary>
    /// The exception that is thrown when a service request is not allowed.
    /// </summary>
    [SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Provided parameters must be present.")]
    public class RequestNotAllowedException: Exception
    {
        internal RequestNotAllowedException(IServiceId requestor, IServiceId requested, string reason) : base(reason) 
        {
            Data.Add(nameof(requestor), requestor.FriendlyName());
            Data.Add(nameof(requested), requested.FriendlyName());
        }
    }
}
