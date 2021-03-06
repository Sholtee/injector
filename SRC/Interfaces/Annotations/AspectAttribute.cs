﻿/********************************************************************************
* AspectAttribute.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Defines an abstract aspect that can be applied on a service interface.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface)]
    public abstract class AspectAttribute : Attribute
    {
        /// <summary>
        /// The kind of this aspect.
        /// </summary>
        public AspectKind Kind { get; protected set; } = AspectKind.Service;

        /// <summary>
        /// Returns a InterfaceInterceptor descendant to the specified interface.
        /// </summary>
        /// <param name="iface">The interface on which this attribute was applied.</param>
        public virtual Type GetInterceptorType(Type iface) => throw new NotImplementedException();

        /// <summary>
        /// Decorates the given service instance. 
        /// </summary>
        /// <param name="injector">The injector on which the service request was initiated.</param>
        /// <param name="iface">The service type.</param>
        /// <param name="instance">The service instance to be decorated.</param>
        /// <returns>The decorated service instance.</returns>
        public virtual object GetInterceptor(IInjector injector, Type iface, object instance) => throw new NotImplementedException();
    }
}
