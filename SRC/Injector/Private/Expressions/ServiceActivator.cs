/********************************************************************************
* ServiceActivator.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;
    using Properties;
    using Proxy.Generators;

    internal static class ServiceActivator
    {
        private static readonly MethodInfo
            //
            // Csak kifejezesek, nem tenyleges metodus hivas
            //

            FInjectorGet     = MethodInfoExtractor.Extract<IInjector>(i => i.Get(null!, null)),
            FInjectorTryGet  = MethodInfoExtractor.Extract<IInjector>(i => i.TryGet(null!, null)),
            FDictTryGetValue = MethodInfoExtractor.Extract<IReadOnlyDictionary<string, object?>, object?>((dict, outVal) => dict.TryGetValue(default!, out outVal)),
            FCreateLazy      = MethodInfoExtractor.Extract(() => CreateLazy<object>(null!, null)).GetGenericMethodDefinition(),
            FCreateLazyOpt   = MethodInfoExtractor.Extract(() => CreateLazyOpt<object>(null!, null)).GetGenericMethodDefinition();

        private static Type? GetEffectiveType(Type type, out bool isLazy)
        {
            if (type.IsInterface)
            {
                isLazy = false;
                return type;
            }

            if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Lazy<>))
            {
                type = type.GetGenericArguments().Single();
                if (type.IsInterface)
                {
                    isLazy = true;
                    return type;
                }
            }

            isLazy = false;
            return null;
        }

        private static void EnsureCanBeInstantiated(Type type) 
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            if (!type.IsClass)
                throw new ArgumentException(Resources.PARAMETER_NOT_A_CLASS, nameof(type));

            if (type.IsAbstract)
                throw new ArgumentException(Resources.PARAMETER_IS_ABSTRACT, nameof(type));

            if (type.IsGenericTypeDefinition)
                throw new ArgumentException(Resources.PARAMETER_IS_GENERIC, nameof(type));
        }

        /// <summary>
        /// <code>
        /// new TClass(..., ..., ...)
        /// {
        ///   Prop_1 = ...,
        ///   Prop_2 = ...
        /// }
        /// </code>
        /// </summary>
        private static Expression New
        (
            ConstructorInfo constructor,
            ParameterExpression injector,
            Func<ParameterExpression, Type, string, OptionsAttribute?, Expression> resolveDep
        ) => Expression.MemberInit
        (
            Expression.New
            (
                constructor,
                constructor
                    .GetParameters()
                    .Select
                    (
                        param => resolveDep
                        (
                            injector,
                            param.ParameterType,
                            param.Name,
                            param.GetCustomAttribute<OptionsAttribute>()
                        )
                    )
            ),
            constructor
                .ReflectedType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.FlattenHierarchy)
                .Where(property => property.GetCustomAttribute<InjectAttribute>() is not null)
                .Select
                (
                    property => Expression.Bind
                    (
                        property,
                        resolveDep
                        (
                            injector,
                            property.PropertyType,
                            property.Name,
                            property.GetCustomAttribute<OptionsAttribute>()
                        )
                    )
                )
        );

        /// <summary>
        /// <code>
        /// (TInterface) injector.[Try]Get(typeof(IInterface), svcName)
        /// // or
        /// Lazy&lt;IInterface&gt;(() => (IInterface) injector.[Try]Get(typeof(IInterface), svcName))
        /// </code>
        /// </summary>
        private static Expression DefaultDependencyResolver(ParameterExpression injector, Type type, string __, OptionsAttribute? options)
        {
            type = GetEffectiveType(type, out bool isLazy) ?? throw new ArgumentException(Resources.INVALID_CONSTRUCTOR);

            return isLazy
                //
                // Lazy<IInterface>(() => (IInterface) injector.[Try]Get(typeof(IInterface), svcName))
                //

                ? CreateLazy(injector, type, options)

                //
                // (TInterface) injector.[Try]Get(typeof(IInterface), svcName)
                //

                : GetService(injector, type, options);
        }

        private static Expression<TDelegate> CreateActivator<TDelegate>(Func<IReadOnlyList<ParameterExpression>, Expression> createInstance, params ParameterExpression[] variables) where TDelegate : Delegate
        {
            List<ParameterExpression> paramz = new
            (
                typeof(TDelegate)
                    .GetMethod(nameof(Action.Invoke))
                    .GetParameters()
                    .Select(static para => Expression.Parameter(para.ParameterType, para.Name))
            );

            Expression<TDelegate> resolver = Expression.Lambda<TDelegate>
            (
                Expression.Block
                (
                    variables,
                    Expression.Convert
                    (
                        createInstance(paramz),
                        typeof(object)
                    )
                ),
                paramz
            );

            Debug.WriteLine($"Created activator:{Environment.NewLine}{resolver.GetDebugView()}");

            return resolver;
        }

        private static Expression<TDelegate> CreateActivator<TDelegate>
        (
            ConstructorInfo constructor,
            Func<ParameterExpression, Type, string, OptionsAttribute?, Expression> resolveDep,
            params ParameterExpression[] variables
        ) where TDelegate : Delegate => CreateActivator<TDelegate>
        (
            paramz => New
            (
                constructor,
                paramz.Single(static param => param.Type == typeof(IInjector)),
                resolveDep
            ),
            variables
        );

        /// <summary>
        /// <code>(injector, iface) => (object) new Service(IDependency_1 | Lazy&lt;IDependency_1&gt;, IDependency_2 | Lazy&lt;IDependency_2&gt;,...)</code>
        /// </summary>
        public static Expression<FactoryDelegate> Get(ConstructorInfo constructor) => CreateActivator<FactoryDelegate>
        (
            constructor, 
            DefaultDependencyResolver
        );

        /// <summary>
        /// <code>(injector, iface) => (object) new Service(IDependency_1 | Lazy&lt;IDependency_1&gt;, IDependency_2 | Lazy&lt;IDependency_2&gt;,...)</code>
        /// </summary>
        public static Expression<FactoryDelegate> Get(Type type)
        {
            //
            // Itt validaljunk ne a hivo oldalon (kodduplikalas elkerulese vegett).
            //

            EnsureCanBeInstantiated(type);

            return Get(type.GetApplicableConstructor());
        }

        /// <summary>
        /// <code>
        /// (injector, explicitArgs) =>
        /// {
        ///    object arg;
        ///    return new Service(explicitArgs.TryGetValue(paramName, out arg) ? arg : Lazy&lt;IDependency_1&gt;) | injector.[Try]Get(typeof(IDependency_1)), ...);
        /// }
        /// </code>
        /// </summary>
        public static Expression<FactoryDelegate> Get(ConstructorInfo constructor, IReadOnlyDictionary<string, object?> explicitArgs)
        {
            ParameterExpression arg = Expression.Variable(typeof(object), nameof(arg));

            return CreateActivator<FactoryDelegate>
            (
                constructor,
                ResolveDependency,
                arg
            );

            Expression ResolveDependency(ParameterExpression injector, Type type, string name, OptionsAttribute? options)
            {
                //
                // Itt nem lehet forditas idoben validalni hogy "type" megfelelo tipus e (nem interface parameter szerepelhet
                // az explicit ertekek kozt). Injector.Get() ugy is szolni fog kesobb ha gond van.
                //

                Type effectiveType = GetEffectiveType(type, out bool isLazy) ?? type;

                //
                // explicitArgs.TryGetValue(name, out explicitVal)
                //   ? (TInterface) explicitVal 
                //   : Lazy<IDependency_1>() | (TInterface) injector.[Try]Get(typeof(IDependency_1))
                //

                return Expression.Condition
                (
                    test: Expression.Call
                    (
                        Expression.Constant(explicitArgs), 
                        FDictTryGetValue, 
                        Expression.Constant(name), 
                        arg
                    ),
                    ifTrue: Expression.Convert(arg, type),
                    ifFalse: isLazy
                        //
                        // Lazy<IInterface>(() => (IInterface) injector.[Try]Get(typeof(IInterface), svcName))
                        //

                        ? CreateLazy(injector, effectiveType, options)

                        //
                        // (TInterface) injector.[Try]Get(typeof(IInterface), svcName)
                        //

                        : GetService(injector, type, options)
                );
            }
        }

        /// <summary>
        /// <code>
        /// (injector, explicitArgs) =>
        /// {
        ///    object arg;
        ///    return new Service(explicitArgs.TryGetValue(paramName, out arg) ? arg : Lazy&lt;IDependency_1&gt;) | injector.[Try]Get(typeof(IDependency_1)), ...);
        /// }
        /// </code>
        /// </summary>
        public static Expression<FactoryDelegate> Get(Type type, IReadOnlyDictionary<string, object?> explicitArgs)
        {
            //
            // Itt validaljunk ne a hivo oldalon (kodduplikalas elkerulese vegett).
            //

            EnsureCanBeInstantiated(type);

            return Get(type.GetApplicableConstructor(), explicitArgs);
        }

        /// <summary>
        /// <code>
        /// (injector, explicitArgs) => (object) new Service(((ParamzProvider) explicitArgs).argName_1 | Lazy&lt;IDependency_1&gt;() | injector.[Try]Get(typeof(IDependency_1)), ...);
        /// </code>
        /// </summary>
        public static Expression<FactoryDelegate> Get(ConstructorInfo constructor, object paramzProvider)
        {
            Type paramzProviderType = paramzProvider.GetType();

            return CreateActivator<FactoryDelegate>
            (
                constructor,
                ResolveDependency
            );

            Expression ResolveDependency(ParameterExpression injector, Type type, string name, OptionsAttribute? options)
            {
                PropertyInfo? valueProvider = paramzProviderType.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

                if (valueProvider?.CanRead is true && type.IsAssignableFrom(valueProvider.PropertyType))
                    //
                    // explicitArgs.argName_1 
                    //

                    return Expression.Property
                    (
                        Expression.Constant(paramzProvider, paramzProviderType),
                        valueProvider
                    );

                type = GetEffectiveType(type, out bool isLazy) ?? throw new ArgumentException(Resources.INVALID_CONSTRUCTOR, nameof(constructor));

                return isLazy
                    //
                    // Lazy<IInterface>(() => (IInterface) injector.[Try]Get(typeof(IInterface), svcName))
                    //

                    ? CreateLazy(injector, type, options)

                    //
                    // (TInterfcae) injector.[Try]Get(typeof(IInterface), svcName)
                    //

                    : GetService(injector, type, options);
            }
        }

        /// <summary>
        /// <code>
        /// (injector, explicitArgs) => (object) new Service(((ParamzProvider) explicitArgs).argName_1 | Lazy&lt;IDependency_1&gt;() | injector.[Try]Get(typeof(IDependency_1)), ...);
        /// </code>
        /// </summary>
        public static Expression<FactoryDelegate> Get(Type type, object paramzProvider)
        {
            EnsureCanBeInstantiated(type);
            return Get(type.GetApplicableConstructor(), paramzProvider);
        }

        private static Expression GetService(ParameterExpression injector, Type iface, OptionsAttribute? options) => Expression.Convert
        (
            Expression.Call
            (
                injector,
                options?.Optional is true ? FInjectorTryGet : FInjectorGet,
                Expression.Constant(iface),
                Expression.Constant(options?.Name, typeof(string))
            ),
            iface
        );

        private static Lazy<TService> CreateLazy<TService>(IInjector injector, string? name) => new Lazy<TService>(() => (TService) injector.Get(typeof(TService), name));

        private static Lazy<TService> CreateLazyOpt<TService>(IInjector injector, string? name) => new Lazy<TService>(() => (TService) injector.TryGet(typeof(TService), name)!);

        internal static Expression CreateLazy(ParameterExpression injector, Type iface, OptionsAttribute? options)
        {
            if (!iface.IsInterface)
                throw new ArgumentException(Resources.PARAMETER_NOT_AN_INTERFACE, nameof(iface));

            //
            // According to ServiceActivator_Lazy perf tests, runtime built lambdas containing a nested function
            // are ridiculously slow (I suspect the nested lambda is instantiated by an Activator.CreateInstance
            // call).
            //

            /*
            Type delegateType = typeof(Func<>).MakeGenericType(iface);

            //
            // () => (iface) injector.[Try]Get(iface, svcName)
            //

            LambdaExpression valueFactory = Expression.Lambda
            (
                delegateType,
                GetService(injector, iface, options)
            );

            //
            // new Lazy<iface>(() => (iface) injector.[Try]Get(iface, svcName))
            //

            Type lazyType = typeof(Lazy<>).MakeGenericType(iface);

            return Expression.New
            (
                lazyType.GetConstructor(new[] { delegateType }) ?? throw new MissingMethodException(lazyType.Name, ConstructorInfo.ConstructorName),
                valueFactory
            );
            */

            //
            // This workaround solves the above mentioned issue but suppresses the ServiceRequestReplacerVisitor.
            // Altough it shouldn't matter as Lazy pattern is for services having considerable instatiation time.
            //

            MethodInfo createLazy = options?.Optional is true
                ? FCreateLazyOpt
                : FCreateLazy;

            return Expression.Call
            (
                createLazy.MakeGenericMethod(iface),
                injector,
                Expression.Constant(options?.Name, typeof(string))
            );
        }

        /// <summary>
        /// <code>
        /// (injector, current) => new GeneratedProxy // AspectAggregator&lt;TInterface, TTarget&gt;
        /// (
        ///   (TTarget) current,
        ///   new IInterfaceInterceptor[]
        ///   {
        ///     new Interceptor_1(injector.Get&lt;IDep_1&gt;(), injector.Get&lt;IDep_2&gt;()),
        ///     new Interceptor_2(injector.Get&lt;IDep_3&gt;())
        ///   }
        /// ); 
        /// </code>
        /// </summary>
        public static Expression<ApplyProxyDelegate> InterceptorsToProxyDelegate(Type iface, Type target, IEnumerable<Type> interceptorTypes)
        {
            Type concreteProxy = new ProxyGenerator
            (
                iface,
                typeof(AspectAggregator<,>).MakeGenericType(iface, target)
            ).GetGeneratedType();

            return CreateActivator<ApplyProxyDelegate>
            (
                paramz => Expression.New
                (
                    concreteProxy.GetApplicableConstructor(),
                    Expression.Convert(paramz[2], target),
                    Expression.NewArrayInit
                    (
                        typeof(IInterfaceInterceptor),
                        interceptorTypes.Select
                        (
                            interceptorType => New
                            (
                                interceptorType.GetApplicableConstructor(),
                                paramz[0],
                                DefaultDependencyResolver
                            )
                        )
                    )
                )
            );
        }

        /// <summary>
        /// <code>
        /// (injector, current) => new GeneratedProxy // AspectAggregator&lt;TInterface, TTarget&gt;
        /// (
        ///   (TTarget) current,
        ///   new IInterfaceInterceptor[]
        ///   {
        ///     new Interceptor_1(injector.Get&lt;IDep_1&gt;(), injector.Get&lt;IDep_2&gt;()),
        ///     new Interceptor_2(injector.Get&lt;IDep_3&gt;())
        ///   }
        /// ); 
        /// </code>
        /// </summary>
        public static Expression<ApplyProxyDelegate>? AspectsToProxyDelegate(Type iface, Type target)
        {
            IEnumerable<Type> interceptors = GetInterceptors(iface);
            if (target != iface)
                interceptors = interceptors.Union(GetInterceptors(target));

            return interceptors.Any()
                ? InterceptorsToProxyDelegate
                (
                    iface,
                    target,
                    interceptors
                )
                : null;

            static IEnumerable<Type> GetInterceptors(Type type)
            {
                IEnumerable<Type> interceptors = type
                   .GetCustomAttributes()
                   .OfType<IAspect>()
                   .Select(static aspect => aspect.UnderlyingInterceptor);

                foreach (Type interceptor in interceptors)
                {
                    if (!typeof(IInterfaceInterceptor).IsAssignableFrom(interceptor))
                        throw new InvalidOperationException(Resources.NOT_AN_INTERCEPTOR);
                    yield return interceptor;
                }
            }
        }
    }
}