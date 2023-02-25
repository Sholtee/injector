/********************************************************************************
* Delegates.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Unconditionaly creates a particular service.
    /// </summary>
    public delegate object CreateServiceDelegate(IServiceActivator scope, out object? disposable);

    /// <summary>
    /// Defines the layout of functions used to apply proxies.
    /// </summary>
    public delegate object DecoratorDelegate(IInjector scope, Type iface, object instance);

    /// <summary>
    /// Defines the layout of functions used to apply proxies.
    /// </summary>
    public delegate TInterface DecoratorDelegate<TInterface>(IInjector scope, TInterface instance);

    /// <summary>
    /// Defines the layout of functions used as service factory.
    /// </summary>
    public delegate object FactoryDelegate(IInjector scope, Type iface);

    /// <summary>
    /// Defines the layout of functions used as service factory.
    /// </summary>
    public delegate TInterface FactoryDelegate<TInterface>(IInjector scope) where TInterface : class;

    /// <summary>
    /// Invokes the next member of chained items.
    /// </summary>
    public delegate T Next<T>();

    /// <summary>
    /// Factory responsible for create interceptors.
    /// </summary>
    public delegate IInterfaceInterceptor CreateInterceptorDelegate(IInjector scope);
}
