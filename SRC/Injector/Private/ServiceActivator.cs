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

    internal static class ServiceActivator
    {
        private static readonly MethodInfo
            //
            // Csak kifejezesek, nem tenyleges metodus hivas
            //

            InjectorGet     = MethodInfoExtractor.Extract<IInjector>(i => i.Get(null!, null)),
            InjectorTryGet  = MethodInfoExtractor.Extract<IInjector>(i => i.TryGet(null!, null)),
            LazyGet         = MethodInfoExtractor.Extract(() => LazyGetImpl<object>(null!, null)).GetGenericMethodDefinition(),
            LazyTryGet      = MethodInfoExtractor.Extract(() => LazyTryGetImpl<object>(null!, null)).GetGenericMethodDefinition(),
            DictTryGetValue = MethodInfoExtractor.Extract<IReadOnlyDictionary<string, object?>, object?>((dict, outVal) => dict.TryGetValue(default!, out outVal));

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
            Ensure.Parameter.IsNotNull(type, nameof(type));
            Ensure.Parameter.IsClass(type, nameof(type));
            Ensure.Parameter.IsNotAbstract(type, nameof(type));
            Ensure.Parameter.IsNotGenericDefinition(type, nameof(type));
        }

        private static TDelegate CreateActivator<TDelegate>(ConstructorInfo constructor, Func<Type, string, OptionsAttribute?, Expression> dependencyResolver, ParameterExpression[] variables, ParameterExpression[] parameters)
        {
            Expression<TDelegate> resolver = Expression.Lambda<TDelegate>
            (
                Expression.Block
                (
                    variables,
                    Expression.Convert
                    (
                        Expression.MemberInit
                        (
                            Expression.New
                            (
                                constructor,
                                constructor
                                    .GetParametersSafe()
                                    .Select
                                    (
                                        param => dependencyResolver(param.ParameterType, param.Name, param.GetCustomAttribute<OptionsAttribute>())
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
                                        dependencyResolver(property.PropertyType, property.Name, property.GetCustomAttribute<OptionsAttribute>())
                                    )
                                )
                        ),
                        typeof(object)
                    )
                ),
                parameters
            );

            Debug.WriteLine($"Created activator:{Environment.NewLine}{resolver.GetDebugView()}");

            return resolver.Compile();
        }

        public static Func<IInjector, Type, object> Get(ConstructorInfo constructor) => Cache.GetOrAdd(constructor, () =>
        {
            //
            // (injector, iface)  => (object) new Service(IDependency_1 | Lazy<IDependency_1>, IDependency_2 | Lazy<IDependency_2>,...)
            //

            ParameterExpression 
                injector = Expression.Parameter(typeof(IInjector), nameof(injector)),
                iface    = Expression.Parameter(typeof(Type),      nameof(iface));

            return CreateActivator<Func<IInjector, Type, object>>
            (
                constructor, 
                ResolveDependency,
                Array.Empty<ParameterExpression>(),
                new[]
                {
                    injector,
                    iface
                }
            );

            Expression ResolveDependency(Type type, string _, OptionsAttribute? options) 
            {
                type = GetEffectiveType(type, out bool isLazy) ?? throw new ArgumentException(Resources.INVALID_CONSTRUCTOR, nameof(constructor));

                return isLazy
                    //
                    // Lazy<IInterface>(() => (IInterface) injector.[Try]Get(typeof(IInterface), svcName))
                    //

                    ? Expression.Invoke
                    (
                        Expression.Constant
                        (
                            GetLazyFactory(type, options)
                        ),
                        injector
                    )

                    //
                    // (TInterface) injector.[Try]Get(typeof(IInterface), svcName)
                    //

                    : Expression.Convert
                    (
                        Expression.Call
                        (
                            injector,
                            options?.Optional is true ? InjectorTryGet : InjectorGet,
                            Expression.Constant(type),
                            Expression.Constant(options?.Name, typeof(string))
                        ),
                        type
                    );
            }
        });

        public static Func<IInjector, Type, object> Get(Type type)
        {
            //
            // Itt validaljunk ne a hivo oldalon (kodduplikalas elkerulese vegett).
            //

            EnsureCanBeInstantiated(type);

            return Cache.GetOrAdd(type, () => Get(type.GetApplicableConstructor()));
        }

        public static Func<IInjector, IReadOnlyDictionary<string, object?>, object> GetExtended(ConstructorInfo constructor) => Cache.GetOrAdd(constructor, () =>
        {
            //
            // (injector, explicitArgs) =>
            // {
            //    object arg;
            //    return new Service(explicitArgs.TryGetValue(paramName, out arg) ? arg : Lazy<IDependency_1>) | injector.[Try]Get(typeof(IDependency_1)), ...);
            // }
            //

            ParameterExpression
                injector     = Expression.Parameter(typeof(IInjector), nameof(injector)),
                explicitArgs = Expression.Parameter(typeof(IReadOnlyDictionary<string, object?>), nameof(explicitArgs)),
                arg          = Expression.Variable(typeof(object), nameof(arg));

            return CreateActivator<Func<IInjector, IReadOnlyDictionary<string, object?>, object>>
            (
                constructor,
                ResolveDependency,
                new[] { arg },
                new[]
                {
                    injector,
                    explicitArgs
                }
            );

            Expression ResolveDependency(Type type, string name, OptionsAttribute? options)
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
                        explicitArgs, 
                        DictTryGetValue, 
                        Expression.Constant(name), 
                        arg
                    ),
                    ifTrue: Expression.Convert(arg, type),
                    ifFalse: isLazy
                        //
                        // Lazy<IInterface>(() => (IInterface) injector.[Try]Get(typeof(IInterface), svcName))
                        //

                        ? Expression.Invoke
                        (
                            Expression.Constant
                            (
                                GetLazyFactory(effectiveType, options)
                            ),
                            injector
                        )

                        //
                        // (TInterface) injector.[Try]Get(typeof(IInterface), svcName)
                        //

                        : Expression.Convert
                        (
                            Expression.Call
                            (
                                injector,
                                options?.Optional is true ? InjectorTryGet : InjectorGet,
                                Expression.Constant(type),
                                Expression.Constant(options?.Name, typeof(string))
                            ),
                            type
                        )
                );
            }
        });

        public static Func<IInjector, IReadOnlyDictionary<string, object?>, object> GetExtended(Type type) => Cache.GetOrAdd(type, () => 
        {
            //
            // Itt validaljunk ne a hivo oldalon (kodduplikalas elkerulese vegett).
            //

            EnsureCanBeInstantiated(type);

            return GetExtended(type.GetApplicableConstructor());
        });

        public static Func<IInjector, object, object> GetExtended(ConstructorInfo constructor, Type paramzProvider) => Cache.GetOrAdd(new { constructor, paramzProvider }, () =>
        {
            //
            // (injector, explicitArgs) =>
            //    return (object) new Service(((ParamzProvider) explicitArgs).argName_1 | Lazy<IDependency_1>() | injector.[Try]Get(typeof(IDependency_1)), ...);
            //

            ParameterExpression
                injector = Expression.Parameter(typeof(IInjector), nameof(injector)),
                explicitArgs = Expression.Parameter(typeof(object), nameof(explicitArgs));

            return CreateActivator<Func<IInjector, object, object>>
            (
                constructor,
                ResolveDependency,
                Array.Empty<ParameterExpression>(),
                new[]
                {
                    injector,
                    explicitArgs
                }
            );

            Expression ResolveDependency(Type type, string name, OptionsAttribute? options)
            {
                PropertyInfo? valueProvider = paramzProvider.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

                if (valueProvider?.CanRead is true && type.IsAssignableFrom(valueProvider.PropertyType))
                    //
                    // ((ParamzProvider) explicitArgs).argName_1 
                    //

                    return Expression.Property
                    (
                        Expression.Convert(explicitArgs, paramzProvider),
                        valueProvider
                    );

                type = GetEffectiveType(type, out bool isLazy) ?? throw new ArgumentException(Resources.INVALID_CONSTRUCTOR, nameof(constructor));

                if (isLazy)
                    //
                    // Lazy<IInterface>(() => (IInterface) injector.[Try]Get(typeof(IInterface), svcName))
                    //

                    return Expression.Invoke
                    (
                        Expression.Constant
                        (
                            GetLazyFactory(type, options)
                        ),
                        injector
                    );

                //
                // (TInterfcae) injector.[Try]Get(typeof(IInterface), svcName)
                //

                return Expression.Convert
                (
                    Expression.Call
                    (
                        injector,
                        options?.Optional is true ? InjectorTryGet : InjectorGet,
                        Expression.Constant(type),
                        Expression.Constant(options?.Name, typeof(string))
                    ),
                    type
                );
            }
        });

        public static Func<IInjector, object, object> GetExtended(Type type, Type paramzProvider) => Cache.GetOrAdd(new { type, paramzProvider }, () =>
        {
            EnsureCanBeInstantiated(type);

            return GetExtended(type.GetApplicableConstructor(), paramzProvider);
        });

        public static Delegate GetLazyFactory(Type iface, OptionsAttribute? options) => Cache.GetOrAdd((iface, options?.Name, options?.Optional), () =>
        {
            Ensure.Parameter.IsInterface(iface, nameof(iface));
            Ensure.Parameter.IsNotGenericDefinition(iface, nameof(iface));

            MethodInfo lazyFactory = (options?.Optional is true ? LazyTryGet : LazyGet).MakeGenericMethod(iface);

            ParameterExpression injector = Expression.Parameter(typeof(IInjector), nameof(injector));

            Type factoryType = typeof(Func<,>).MakeGenericType
            (
                typeof(IInjector), 
                typeof(Lazy<>).MakeGenericType(iface)
            );

            return Expression.Lambda
            (
                factoryType,
                Expression.Call
                (
                    lazyFactory,
                    injector,
                    Expression.Constant(options?.Name, typeof(string))
                ),
                injector
            ).Compile();
        });

        private static Lazy<TInterface> LazyGetImpl<TInterface>(IInjector injector, string? name) where TInterface : class => new Lazy<TInterface>(() => (TInterface) injector.Get(typeof(TInterface), name));

        private static Lazy<TInterface?> LazyTryGetImpl<TInterface>(IInjector injector, string? name) where TInterface : class => new Lazy<TInterface?>(() => (TInterface?) injector.TryGet(typeof(TInterface), name));
    }
}