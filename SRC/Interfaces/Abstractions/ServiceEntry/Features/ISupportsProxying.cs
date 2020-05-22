/********************************************************************************
* ISupportsProxying.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Provides the mechanism for overriding the factory function. 
    /// </summary>
    public interface  ISupportsProxying
    {
        /// <summary>
        /// The factory function that can be overridden.
        /// </summary>
        Func<IInjector, Type, object>? Factory { get;  set; }
    }
}
