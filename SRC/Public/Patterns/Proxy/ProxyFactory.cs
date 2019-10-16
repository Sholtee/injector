/********************************************************************************
* ProxyFactory.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Solti.Utils.DI.Proxy
{
    using Properties;
    using Internals;

    /// <summary>
    /// Defines mechanisms to create proxy objects.
    /// </summary>
    public static class ProxyFactory
    {
        /// <summary>
        /// Creates a new proxy instance with the given arguments.
        /// </summary>
        /// <typeparam name="TInterface">The interface to be intercepted.</typeparam>
        /// <typeparam name="TInterceptor">The interceptor class.</typeparam>
        /// <param name="argTypes">Types of arguments passed to the constructor of <typeparamref name="TInterceptor"/></param>
        /// <param name="args">Arguments to be passed to the constructor of <typeparamref name="TInterceptor"/></param>
        /// <returns>The newly created proxy instance.</returns>
        public static TInterface Create<TInterface, TInterceptor>(Type[] argTypes, params object[] args) where TInterface : class where TInterceptor : InterfaceInterceptor<TInterface> => (TInterface) 
            GeneratedProxy<TInterface, TInterceptor>.Type.CreateInstance(argTypes, args);

        /// <summary>
        /// Creates a new proxy instance with the given target.
        /// </summary>
        /// <typeparam name="TInterface">The interface to be intercepted.</typeparam>
        /// <typeparam name="TInterceptor">The interceptor class.</typeparam>
        /// <param name="target">The target of the proxy.</param>
        /// <returns>The newly created proxy instance.</returns>
        public static TInterface Create<TInterface, TInterceptor>(TInterface target) where TInterface : class where TInterceptor : InterfaceInterceptor<TInterface> => 
            Create<TInterface, TInterceptor>(new[]{typeof(TInterface)}, target);

        /// <summary>
        /// Creates a new proxy instance with the given injector.
        /// </summary>
        /// <typeparam name="TInterface">The interface to be intercepted.</typeparam>
        /// <typeparam name="TInterceptor">The interceptor class.</typeparam>
        /// <param name="target">The target of the proxy.</param>
        /// <param name="injector">The injector to resolve the dependencies of the proxy.</param>
        /// <param name="targetParamName">Parameter name of the target (usually "target").</param>
        /// <returns>The newly created proxy instance.</returns>
        public static TInterface Create<TInterface, TInterceptor>(TInterface target, IInjector injector, string targetParamName = "target") where TInterface : class where TInterceptor : InterfaceInterceptor<TInterface> => (TInterface) 
            injector.Instantiate(GeneratedProxy<TInterface, TInterceptor>.Type, new Dictionary<string, object>
            {
                {targetParamName, target}
            });

        /// <summary>
        /// Creates a new proxy instance with the given injector (without target).
        /// </summary>
        /// <typeparam name="TInterface">The interface to be intercepted.</typeparam>
        /// <typeparam name="TInterceptor">The interceptor class.</typeparam>
        /// <param name="injector">The injector to resolve the dependencies of the proxy.</param>
        /// <returns>The newly created proxy instance.</returns>
        public static TInterface Create<TInterface, TInterceptor>(IInjector injector) where TInterface : class where TInterceptor : InterfaceInterceptor<TInterface> => (TInterface) 
            injector.Instantiate(GeneratedProxy<TInterface, TInterceptor>.Type);

        internal static Type GetGeneratedProxyType(Type iface, Type interceptor)
        {
            if (!iface.IsInterface())
                throw new ArgumentException(Resources.NOT_AN_INTERFACE, nameof(iface));

            if (!typeof(InterfaceInterceptor<>).MakeGenericType(iface).IsAssignableFrom(interceptor))
                throw new ArgumentException(Resources.INVALID_INTERCEPETOR, nameof(interceptor));

            return Cache<object, Type>.GetOrAdd(new { iface, interceptor }, () => (Type) typeof(GeneratedProxy<,>)
                .MakeGenericType(iface, interceptor)
#if NETSTANDARD1_6
                .GetProperty(nameof(Type), BindingFlags.Public | BindingFlags.Static)
                .GetValue(null)
#else
                .InvokeMember(nameof(Type), BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty, null, null, new object[0])
#endif
            );   
            
        }
    }
}
