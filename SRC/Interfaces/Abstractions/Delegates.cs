/********************************************************************************
* Delegates.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Gets or creates a particular service.
    /// </summary>
    public delegate object ResolveServiceDelegate(IServiceFactory instanceFactory);  // TODO: REMOVE

    /// <summary>
    /// Unconditionaly creates a particular service.
    /// </summary>
    public delegate object CreateServiceDelegate(IServiceFactory scope, out object? disposable);

    /// <summary>
    /// Defines the layout of functions used to apply proxies.
    /// </summary>
    public delegate object DecoratorDelegate(IInjector scope, Type iface, object instance);

    /// <summary>
    /// Defines the layout of functions used as service factory.
    /// </summary>
    public delegate object FactoryDelegate(IInjector scope, Type iface);

    /// <summary>
    /// Defines the layout of functions used as service factory.
    /// </summary>
    public delegate TInterface FactoryDelegate<TInterface>(IInjector scope) where TInterface : class;

    /// <summary>
    /// Defines an interceptor invocation.
    /// </summary>
    public delegate object? InvokeInterceptorDelegate();
}
