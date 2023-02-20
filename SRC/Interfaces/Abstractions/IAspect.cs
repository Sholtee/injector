/********************************************************************************
* IAspect.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Defines the contract of aspects.
    /// </summary>
    public interface IAspect
    {
        /// <summary>
        /// The underlying interceptor <see cref="Type"/> that implements the <see cref="IInterfaceInterceptor"/> interface.
        /// </summary>
        Type UnderlyingInterceptor { get; }

        /// <summary>
        /// Explicit arguments to be passed:
        /// <code>Explicitrgs = new {ctorParamName1 = ..., ctorParamName2 = ...}</code>
        /// </summary>
        object? ExplicitArgs { get; }
    }
}