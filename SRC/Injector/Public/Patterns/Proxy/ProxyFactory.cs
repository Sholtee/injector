﻿/********************************************************************************
* ProxyFactory.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Solti.Utils.Proxy
{
    using Generators;
    using Primitives;

    using DI;
    using DI.Internals;
    using DI.Interfaces;

    /// <summary>
    /// Defines mechanisms to create proxy objects.
    /// </summary>
    public static class ProxyFactory
    {
        /// <summary>
        /// Specifies whether the system should cache the generated proxy assemblies or not.
        /// </summary>
        /// <remarks>This property should be true in production builds.</remarks>
        public static bool PreserveProxyAssemblies { get; set; }

        private static Type GenerateProxyType<TInterface, TInterceptor>() where TInterface : class where TInterceptor : InterfaceInterceptor<TInterface>
        {
            if (PreserveProxyAssemblies)
                TypeGeneratorExtensions.SetCacheDirectory<TInterface, ProxyGenerator<TInterface, TInterceptor>>();

            //
            // Generikus parameterek validalasat a ProxyGenerator<> vegzi.
            //

            #pragma warning disable CS0618 // Type or member is obsolete
            return ProxyGenerator<TInterface, TInterceptor>.GeneratedType;
            #pragma warning restore CS0618
        }

        /// <summary>
        /// Creates a new proxy instance with the given arguments.
        /// </summary>
        /// <typeparam name="TInterface">The interface to be intercepted.</typeparam>
        /// <typeparam name="TInterceptor">The interceptor class. It must be an <see cref="InterfaceInterceptor{TInterface}"/> descendant.</typeparam>
        /// <param name="args">Arguments to be passed to the constructor of the <typeparamref name="TInterceptor"/>.</param>
        /// <returns>The newly created proxy instance.</returns>
        public static TInterface Create<TInterface, TInterceptor>(params object[] args) where TInterface : class where TInterceptor : InterfaceInterceptor<TInterface> => (TInterface) 
            GenerateProxyType<TInterface, TInterceptor>()
                .GetApplicableConstructor()
                .ToStaticDelegate()
                .Invoke(args);

        /// <summary>
        /// Creates a new proxy instance with the given arguments.
        /// </summary>
        /// <typeparam name="TInterface">The interface to be intercepted.</typeparam>
        /// <typeparam name="TInterceptor">The interceptor class. It must be an <see cref="InterfaceInterceptor{TInterface}"/> descendant.</typeparam>
        /// <param name="argTypes">An array of <see cref="Type"/> objects representing the number, order, and type of the parameters of the desired <typeparamref name="TInterceptor"/> constructor.</param>
        /// <param name="args">Arguments to be passed to the constructor of the <typeparamref name="TInterceptor"/>.</param>
        /// <returns>The newly created proxy instance.</returns>
        public static TInterface Create<TInterface, TInterceptor>(Type[] argTypes, params object[] args) where TInterface : class where TInterceptor : InterfaceInterceptor<TInterface> => (TInterface)
            GenerateProxyType<TInterface, TInterceptor>()
                .CreateInstance(argTypes, args);

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
            injector.Instantiate(GenerateProxyType<TInterface, TInterceptor>(), new Dictionary<string, object>
            {
                {targetParamName, target}
            });

        private static readonly MethodInfo FGenericGenerateProxyType = MethodInfoExtractor
            .Extract(() => GenerateProxyType<object, InterfaceInterceptor<object>>())
            .GetGenericMethodDefinition();

        internal static Type GenerateProxyType(Type iface, Type interceptor)
        {
            Ensure.Parameter.IsNotNull(iface, nameof(iface));
            Ensure.Parameter.IsNotNull(interceptor, nameof(interceptor));

            //
            // A tobbit a ProxyGenerator<> ellenorzi.
            //

            return (Type) FGenericGenerateProxyType.MakeGenericMethod(iface, interceptor).ToStaticDelegate().Invoke(Array.Empty<object>());
        }

        /// <summary>
        /// Creates a new proxy instance with the given arguments.
        /// </summary>
        /// <param name="iface">The interface to be intercepted.</param>
        /// <param name="interceptor">The interceptor class. It must be an <see cref="InterfaceInterceptor{TInterface}"/> descendant.</param>
        /// <param name="args">Arguments to be passed to the constructor of the <paramref name="interceptor"/>.</param>
        /// <returns>The newly created proxy instance.</returns>
        public static object Create(Type iface, Type interceptor, params object[] args) => GenerateProxyType(iface, interceptor)
            .GetApplicableConstructor()
            .ToStaticDelegate()
            .Invoke(args);

        /// <summary>
        /// Creates a new proxy instance with the given arguments.
        /// </summary>
        /// <param name="iface">The interface to be intercepted.</param>
        /// <param name="interceptor">The interceptor class. It must be an <see cref="InterfaceInterceptor{TInterface}"/> descendant.</param>
        /// <param name="argTypes">An array of <see cref="Type"/> objects representing the number, order, and type of the parameters of the desired <paramref name="interceptor"/> constructor.</param>
        /// <param name="args">Arguments to be passed to the constructor of the <paramref name="interceptor"/>.</param>
        /// <returns>The newly created proxy instance.</returns>
        public static object Create(Type iface, Type interceptor, Type[] argTypes, params object[] args) => GenerateProxyType(iface, interceptor).CreateInstance(argTypes, args);

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
            injector.Instantiate(GenerateProxyType(iface, interceptor), new Dictionary<string, object>
            {
                {targetParamName, target}
            });
    }
}
