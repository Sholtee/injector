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
    public delegate object DecoratorDelegate(IInjector scope, Type type, object instance);

    /// <summary>
    /// Defines the layout of functions used to apply proxies.
    /// </summary>
    public delegate TType DecoratorDelegate<TType>(IInjector scope, TType instance);

    /// <summary>
    /// Defines the layout of functions used as service factory.
    /// </summary>
    public delegate object FactoryDelegate(IInjector scope, Type type);

    /// <summary>
    /// Defines the layout of functions used as service factory.
    /// </summary>
    public delegate TType FactoryDelegate<TType>(IInjector scope) where TType : class;

    /// <summary>
    /// Invokes the next member of chained items.
    /// </summary>
    public delegate TResult CallNextDelegate<TConext, TResult>(TConext conext);

    /// <summary>
    /// Factory responsible for create interceptors.
    /// </summary>
    public delegate IInterfaceInterceptor CreateInterceptorDelegate(IInjector scope);
}
