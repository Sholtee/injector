/********************************************************************************
* AspectKind.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Specifies the method that will be used to generate the proxy.
    /// </summary>
    public enum AspectKind 
    {
        /// <summary>
        /// The proxy is created from its type returned by the <see cref="AspectAttribute.GetInterceptorType(Type)"/> method.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
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
