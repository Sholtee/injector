/********************************************************************************
* ProxyFactory.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Solti.Utils.Proxy
{
    using Generators;

    using DI;
    using DI.Internals;

    /// <summary>
    /// Defines a mechanisms to create proxy objects.
    /// </summary>
    public static class ProxyFactory
    {
        /// <summary>
        /// Creates a new proxy instance with the given arguments.
        /// </summary>
        /// <typeparam name="TInterface">The interface to be intercepted.</typeparam>
        /// <typeparam name="TInterceptor">The interceptor class. It must be an <see cref="InterfaceInterceptor{TInterface}"/> descendant.</typeparam>
        /// <param name="args">Arguments to be passed to the constructor of the <typeparamref name="TInterceptor"/>.</param>
        /// <returns>The newly created proxy instance.</returns>
        public static TInterface Create<TInterface, TInterceptor>(params object[] args) where TInterface : class where TInterceptor : InterfaceInterceptor<TInterface> =>
            (TInterface) ProxyGenerator<TInterface, TInterceptor>
                .GeneratedType
                .GetApplicableConstructor()
                .Call(args);

        /// <summary>
        /// Creates a new proxy instance with the given arguments.
        /// </summary>
        /// <typeparam name="TInterface">The interface to be intercepted.</typeparam>
        /// <typeparam name="TInterceptor">The interceptor class. It must be an <see cref="InterfaceInterceptor{TInterface}"/> descendant.</typeparam>
        /// <param name="argTypes">An array of <see cref="Type"/> objects representing the number, order, and type of the parameters of the desired <typeparamref name="TInterceptor"/> constructor.</param>
        /// <param name="args">Arguments to be passed to the constructor of the <typeparamref name="TInterceptor"/>.</param>
        /// <returns>The newly created proxy instance.</returns>
        public static TInterface Create<TInterface, TInterceptor>(Type[] argTypes, params object[] args) where TInterface : class where TInterceptor : InterfaceInterceptor<TInterface> => (TInterface) 
            ProxyGenerator<TInterface, TInterceptor>.GeneratedType.CreateInstance(argTypes, args);

        /// <summary>
        /// Creates a new proxy instance with the given target.
        /// </summary>
        /// <typeparam name="TInterface">The interface to be intercepted.</typeparam>
        /// <typeparam name="TInterceptor">The interceptor class. It must be an <see cref="InterfaceInterceptor{TInterface}"/> descendant.</typeparam>
        /// <param name="target">The target of the proxy.</param>
        /// <returns>The newly created proxy instance.</returns>
        public static TInterface Create<TInterface, TInterceptor>(TInterface target) where TInterface : class where TInterceptor : InterfaceInterceptor<TInterface> => 
            Create<TInterface, TInterceptor>(new[]{typeof(TInterface)}, target);

        /// <summary>
        /// Creates a new proxy instance with the given injector.
        /// </summary>
        /// <typeparam name="TInterface">The interface to be intercepted.</typeparam>
        /// <typeparam name="TInterceptor">The interceptor class. It must be an <see cref="InterfaceInterceptor{TInterface}"/> descendant.</typeparam>
        /// <param name="target">The target of the proxy.</param>
        /// <param name="injector">The injector to resolve the dependencies of the proxy.</param>
        /// <param name="targetParamName">Parameter name of the target (usually "target").</param>
        /// <returns>The newly created proxy instance.</returns>
        public static TInterface Create<TInterface, TInterceptor>(TInterface target, IInjector injector, string targetParamName = "target") where TInterface : class where TInterceptor : InterfaceInterceptor<TInterface> => (TInterface) 
            injector.Instantiate(ProxyGenerator<TInterface, TInterceptor>.GeneratedType, new Dictionary<string, object>
            {
                {targetParamName, target}
            });

        private static Type GetGeneratedProxyType(Type iface, Type interceptor)
        {
            Ensure.Parameter.IsNotNull(iface, nameof(iface));
            Ensure.Parameter.IsInterface(iface, nameof(iface)); // TODO: FIXME: ProxyGenerator ervenytelen muveletet dob ha az "iface" parameter nem interface
            Ensure.Type.IsAssignable(typeof(InterfaceInterceptor<>).MakeGenericType(iface), interceptor);

            return Cache.GetOrAdd((iface, interceptor), () => (Type) typeof(ProxyGenerator<,>)
                .MakeGenericType(iface, interceptor)
                .GetProperty(
                    nameof(ProxyGenerator<object, InterfaceInterceptor<object>>.GeneratedType), 
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .GetValue(null));
        }

        /// <summary>
        /// Creates a new proxy instance with the given arguments.
        /// </summary>
        /// <param name="iface">The interface to be intercepted.</param>
        /// <param name="interceptor">The interceptor class. It must be an <see cref="InterfaceInterceptor{TInterface}"/> descendant.</param>
        /// <param name="args">Arguments to be passed to the constructor of the <paramref name="interceptor"/>.</param>
        /// <returns>The newly created proxy instance.</returns>
        public static object Create(Type iface, Type interceptor, params object[] args) => GetGeneratedProxyType(iface, interceptor)
            .GetApplicableConstructor()
            .Call(args);

        /// <summary>
        /// Creates a new proxy instance with the given arguments.
        /// </summary>
        /// <param name="iface">The interface to be intercepted.</param>
        /// <param name="interceptor">The interceptor class. It must be an <see cref="InterfaceInterceptor{TInterface}"/> descendant.</param>
        /// <param name="argTypes">An array of <see cref="Type"/> objects representing the number, order, and type of the parameters of the desired <paramref name="interceptor"/> constructor.</param>
        /// <param name="args">Arguments to be passed to the constructor of the <paramref name="interceptor"/>.</param>
        /// <returns>The newly created proxy instance.</returns>
        public static object Create(Type iface, Type interceptor, Type[] argTypes, params object[] args) => GetGeneratedProxyType(iface, interceptor).CreateInstance(argTypes, args);

        /// <summary>
        /// Creates a new proxy instance with the given target.
        /// </summary>
        /// <param name="iface">The interface to be intercepted.</param>
        /// <param name="interceptor">The interceptor class. It must be an <see cref="InterfaceInterceptor{TInterface}"/> descendant.</param>
        /// <param name="target">The target of the proxy. Must be an <paramref name="iface"/> instance.</param>
        /// <returns>The newly created proxy instance.</returns>
        public static object Create(Type iface, Type interceptor, object target) => Create(iface, interceptor, new[] { iface }, target);

        /// <summary>
        /// Creates a new proxy instance with the given injector.
        /// </summary>
        /// <param name="iface">The interface to be intercepted.</param>
        /// <param name="interceptor">The interceptor class. It must be an <see cref="InterfaceInterceptor{TInterface}"/> descendant.</param>
        /// <param name="target">The target of the proxy.</param>
        /// <param name="injector">The injector to resolve the dependencies of the proxy.</param>
        /// <param name="targetParamName">Parameter name of the target (usually "target").</param>
        /// <returns>The newly created proxy instance.</returns>
        public static object Create(Type iface, Type interceptor, object target, IInjector injector, string targetParamName = "target") => 
            injector.Instantiate(GetGeneratedProxyType(iface, interceptor), new Dictionary<string, object>
            {
                {targetParamName, target}
            });
    }
}
