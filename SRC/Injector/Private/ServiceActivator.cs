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
                                        param => Expression.Convert
                                        (
                                            dependencyResolver(param.ParameterType, param.Name, param.GetCustomAttribute<OptionsAttribute>()),
                                            param.ParameterType
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
                                        Expression.Convert
                                        (
                                            dependencyResolver(property.PropertyType, property.Name, property.GetCustomAttribute<OptionsAttribute>()),
                                            property.PropertyType
                                        )
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
                    // injector.[Try]Get(typeof(IInterface), svcName)
                    //

                    : Expression.Call
                    (
                        injector,
                        options?.Optional is true ? InjectorTryGet : InjectorGet,
                        Expression.Constant(type),
                        Expression.Constant(options?.Name, typeof(string))
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
            // (injector, explicitVals) =>
            // {
            //    object explicitVal;
            //    return new Service(explicitVals.TryGetValue(paramName, out explicitVal) ? explicitVal : Lazy<IDependency_1>) | injector.[Try]Get(typeof(IDependency_1)), ...);
            // }
            //

            ParameterExpression
                injector     = Expression.Parameter(typeof(IInjector), nameof(injector)),
                explicitVals = Expression.Parameter(typeof(IReadOnlyDictionary<string, object?>), nameof(explicitVals)),
                explicitVal  = Expression.Variable(typeof(object), nameof(explicitVal));

            return CreateActivator<Func<IInjector, IReadOnlyDictionary<string, object?>, object>>
            (
                constructor,
                ResolveDependency,
                new[] { explicitVal },
                new[]
                {
                    injector,
                    explicitVals
                }
            );

            Expression ResolveDependency(Type type, string name, OptionsAttribute? options)
            {
                //
                // Itt nem lehet forditas idoben validalni hogy "type" megfelelo tipus e (nem interface parameter szerepelhet
                // az explicit ertekek kozt). Injector.Get() ugy is szolni fog kesobb ha gond van.
                //

                type = GetEffectiveType(type, out bool isLazy) ?? type;

                //
                // explicitVals.TryGetValue(name, out explicitVal)
                //   ? explicitVal 
                //   : Lazy<IDependency_1>) | injector.[Try]Get(typeof(IDependency_1))
                //

                return Expression.Condition
                (
                    test: Expression.Call
                    (
                        explicitVals, 
                        DictTryGetValue, 
                        Expression.Constant(name), 
                        explicitVal
                    ),
                    ifTrue: explicitVal,
                    ifFalse: isLazy
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
                        // injector.[Try]Get(typeof(IInterface), svcName)
                        //

                        : Expression.Call
                        (
                            injector,
                            options?.Optional is true ? InjectorTryGet : InjectorGet,
                            Expression.Constant(type),
                            Expression.Constant(options?.Name, typeof(string))
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

        public static Func<IInjector, object> GetLazyFactory(Type iface, OptionsAttribute? options) => Cache.GetOrAdd((iface, options?.Name, options?.Optional), () =>
        {
            Ensure.Parameter.IsInterface(iface, nameof(iface));
            Ensure.Parameter.IsNotGenericDefinition(iface, nameof(iface));

            Type delegateType = typeof(Func<>).MakeGenericType(iface);

            //
            // () => (iface) injector.[Try]Get(iface, svcName)
            //

            ParameterExpression injector = Expression.Parameter(typeof(IInjector), nameof(injector));

            LambdaExpression valueFactory = Expression.Lambda
            (
                delegateType,
                Expression.Convert
                (
                    Expression.Call
                    (
                        injector,
                        options?.Optional == true ? InjectorTryGet : InjectorGet,
                        Expression.Constant(iface),
                        Expression.Constant(options?.Name, typeof(string))
                    ),
                    iface
                )
            );

            //
            // injector => new Lazy<iface>(() => (iface) injector.[Try]Get(iface, svcName))
            //

            Type lazyType = typeof(Lazy<>).MakeGenericType(iface);

            Expression<Func<IInjector, object>> lazyFactory = Expression.Lambda<Func<IInjector, object>>
            (
                Expression.New
                (
                    lazyType.GetConstructor(new[] { delegateType }) ?? throw new MissingMethodException(lazyType.Name, "Ctor"),
                    valueFactory
                ),
                injector
            );

            Debug.WriteLine($"Created Lazy<> factory:{Environment.NewLine}{lazyFactory.GetDebugView()}");

            return lazyFactory.Compile();
        });
    }
}