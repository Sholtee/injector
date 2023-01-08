/********************************************************************************
* AspectAttribute.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Defines an abstract aspect that can be applied against service interfaces or classes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
    public abstract class AspectAttribute : Attribute, IAspect
    {
        /// <summary>
        /// See <see cref="IAspect.UnderlyingInterceptor"/>
        /// </summary>
        public abstract Type UnderlyingInterceptor { get; }
    }
}
