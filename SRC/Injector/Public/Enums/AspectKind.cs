/********************************************************************************
* AspectKind.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI
{
    /// <summary>
    /// Specifies the method that will be used to generate the proxy.
    /// </summary>
    public enum AspectKind 
    {
        /// <summary>
        /// The proxy is created from its type returned by the <see cref="AspectAttribute.GetInterceptor(Type)"/> method.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>
        /// <description>
        /// Under the hood the system uses the <see cref="IInjectorExtensions.Instantiate(IInjector, Type, IReadOnlyDictionary{string, object}?)"/> method to instantiate the proxy type.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// The original instance can be accessible via the constructor argument named "target".
        /// </description>
        /// </item>
        /// </list>
        /// </remarks>
        Service,

        /// <summary>
        /// The proxy is created by the <see cref="AspectAttribute.GetInterceptor(IInjector, Type, object)"/> method. 
        /// </summary>
        Factory
    }
}
