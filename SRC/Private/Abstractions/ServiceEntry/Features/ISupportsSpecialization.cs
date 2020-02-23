/********************************************************************************
* ISupportsSpecialization.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Provides the mechanism for specializing service entries. 
    /// </summary>
    public interface ISupportsSpecialization
    {
        /// <summary>
        /// Specializes a service entry if it is generic.
        /// </summary>
        AbstractServiceEntry Specialize(params Type[] genericArguments);
    }
}
