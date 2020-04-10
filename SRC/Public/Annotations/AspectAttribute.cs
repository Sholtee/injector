/********************************************************************************
* AspectAttribute.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Proxy;

    /// <summary>
    /// Defines an abstract aspect that can be applied on a service interface.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
    public abstract class AspectAttribute: Attribute
    {
        /// <summary>
        /// Returns the <see cref="InterfaceInterceptor{TInterface}"/> to the specified interface.
        /// </summary>
        /// <param name="iface">The interface on which this attribute was applied.</param>
        public abstract Type GetInterceptor(Type iface);
    }
}
