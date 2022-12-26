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
                    .GetParametersSafe()
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

        private static Expression<TDelegate> CreateActivator<TDelegate>
        (
            ConstructorInfo constructor,
            Func<ParameterExpression, Type, string, OptionsAttribute?, Expression> resolveDep,
            ParameterExpression? additinalParameter,
            params ParameterExpression[] variables
        ) where TDelegate : Delegate
        {
            ParameterExpression
                injector = Expression.Parameter(typeof(IInjector), nameof(injector)),
                iface = Expression.Parameter(typeof(Type), nameof(iface));

            List<ParameterExpression> paramz = new(3)
            {
                injector,
                iface
            };
            if (additinalParameter is not null)
                paramz.Add(additinalParameter);

            Expression<TDelegate> resolver = Expression.Lambda<TDelegate>
            (
                Expression.Block
                (
                    variables,
                    Expression.Convert
                    (
                        New(constructor, injector, resolveDep),
                        typeof(object)
                    )
                ),
                paramz
            );

            Debug.WriteLine($"Created activator:{Environment.NewLine}{resolver.GetDebugView()}");

            return resolver;
        }

        /// <summary>
        /// <code>(injector, iface)  => (object) new Service(IDependency_1 | Lazy&lt;IDependency_1&gt;, IDependency_2 | Lazy&lt;IDependency_2&gt;,...)</code>
        /// </summary>
        public static Expression<FactoryDelegate> Get(ConstructorInfo constructor) => CreateActivator<FactoryDelegate>
        (
            constructor, 
            DefaultDependencyResolver,
            null
        );

        /// <summary>
        /// <code>(injector, objects)  => (object) new Service(explicit | IDependency_1 | Lazy&lt;IDependency_1&gt;, explicit | IDependency_2 | Lazy&lt;IDependency_2&gt;,...)</code>
        /// </summary>
        public static Expression<ApplyProxyDelegate> GetLateBound(ConstructorInfo constructor, int argIndex) // TODO: delete
        {
            int i = 0;
            ParameterExpression explicitArg = Expression.Parameter(typeof(object), nameof(explicitArg));

            return CreateActivator<ApplyProxyDelegate>
            (
                constructor,
                ResolveDependency,
                explicitArg
            );

            Expression ResolveDependency(ParameterExpression injector, Type type, string __, OptionsAttribute? options)
            {
                if (argIndex == i++)
                    return Expression.Convert(explicitArg, type);

                type = GetEffectiveType(type, out bool isLazy) ?? throw new ArgumentException(Resources.INVALID_CONSTRUCTOR, nameof(constructor));

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
        }

        /// <summary>
        /// <code>(injector, iface)  => (object) new Service(IDependency_1 | Lazy&lt;IDependency_1&gt;, IDependency_2 | Lazy&lt;IDependency_2&gt;,...)</code>
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
                null,
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
                ResolveDependency,
                null
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
        public static Expression<ApplyProxyDelegate> ApplyInterceptors(Type iface, Type target, IEnumerable<Type> interceptorTypes)
        {
            Type concreteProxy = new ProxyGenerator
            (
                iface,
                typeof(AspectAggregator<,>).MakeGenericType(iface, target)
            ).GetGeneratedType();

            ParameterExpression
                injector = Expression.Parameter(typeof(IInjector), nameof(injector)),
                current = Expression.Parameter(typeof(object), nameof(current));

            return Expression.Lambda<ApplyProxyDelegate>
            (
                Expression.New
                (
                    concreteProxy.GetConstructors().Single(),
                    Expression.Convert(current, target),
                    Expression.NewArrayInit
                    (
                        typeof(IInterfaceInterceptor),
                        interceptorTypes.Select
                        (
                            interceptorType => New
                            (
                                interceptorType.GetApplicableConstructor(),
                                injector,
                                DefaultDependencyResolver
                            )
                        )
                    )
                ),
                injector,
                current
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
        public static Expression<ApplyProxyDelegate> ApplyAspects(Type iface, Type target) => ApplyInterceptors
        (
            iface,
            target,
            target
                .GetCustomAttributes()
                .OfType<IAspect>()
                .Select(aspect => aspect.UnderlyingInterceptor)
        );
    }
}