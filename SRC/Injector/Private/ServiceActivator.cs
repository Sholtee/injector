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
    using Proxy.Internals;

    internal static class ServiceActivator
    {
        private static object? voidVal;

        private static readonly MethodInfo
            //
            // Csak kifejezesek, nem tenyleges metodus hivas
            //

            InjectorGet     = MethodInfoExtractor.Extract<IInjector>(i => i.Get(null!, null)),
            InjectorTryGet  = MethodInfoExtractor.Extract<IInjector>(i => i.TryGet(null!, null)),
            DictTryGetValue = MethodInfoExtractor.Extract<IReadOnlyDictionary<string, object?>>(dict => dict.TryGetValue(default!, out voidVal));

        private static Type? GetEffectiveParameterType(Type paramType, out bool isLazy)
        {
            if (paramType.IsInterface)
            {
                isLazy = false;
                return paramType;
            }

            if (paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(Lazy<>))
            {
                paramType = paramType.GetGenericArguments().Single();
                if (paramType.IsInterface)
                {
                    isLazy = true;
                    return paramType;
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

        private static TDelegate CreateActivator<TDelegate>(ConstructorInfo constructor, Func<(Type Type, string Name, OptionsAttribute? Options), Expression> argumentResolver, ParameterExpression[] variables, ParameterExpression[] parameters)
        {
            IReadOnlyCollection<ParameterInfo>? ctorParamz = null;        

            if (constructor.DeclaringType.GetCustomAttribute<RelatedGeneratorAttribute>() is not null)
            {
                //
                // Specialis eset amikor proxy-t hozunk letre majd probaljuk aktivalni. Itt a parametereken levo attributumok nem lennenek 
                // lathatok ezert a varazslas.
                //

                Type @base = constructor.DeclaringType.BaseType;
                Debug.Assert(@base is not null);

                ConstructorInfo? baseCtor = @base!.GetConstructor(constructor
                    .GetParameters()
                    .Select(param => param.ParameterType)
                    .ToArray());

                if (baseCtor is not null)
                    ctorParamz = baseCtor.GetParameters();
            }

            ctorParamz ??= constructor.GetParameters();

            Expression<TDelegate> resolver = Expression.Lambda<TDelegate>
            (
                Expression.Block
                (
                    variables,
                    Expression.Convert
                    (
                        Expression.New
                        (
                            constructor,
                            ctorParamz.Select
                            (
                                param => Expression.Convert
                                (
                                    argumentResolver((param.ParameterType, param.Name, param.GetCustomAttribute<OptionsAttribute>())),
                                    param.ParameterType
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
                ResolveArgument,
                Array.Empty<ParameterExpression>(),
                new[]
                {
                    injector,
                    iface
                }
            );

            Expression ResolveArgument((Type Type, string _, OptionsAttribute? Options) param) 
            {
                Type parameterType = GetEffectiveParameterType(param.Type, out bool isLazy) ?? throw new ArgumentException(Resources.INVALID_CONSTRUCTOR, nameof(constructor));

                if (isLazy)
                    //
                    // Lazy<IInterface>(() => (IInterface) injector.[Try]Get(typeof(IInterface), svcName))
                    //

                    return Expression.Invoke
                    (
                        Expression.Constant(GetLazyFactory(parameterType, param.Options)),
                        injector
                    )!;

                //
                // injector.[Try]Get(typeof(IInterface), svcName)
                //

                return Expression.Call
                (
                    injector,
                    param.Options?.Optional == true ? InjectorTryGet : InjectorGet,
                    Expression.Constant(parameterType),
                    Expression.Constant(param.Options?.Name, typeof(string))
                )!;
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
            // (injector, explicitParamz) =>
            // {
            //    object explicitArg;
            //    return new Service(explicitArgs.TryGetValue(paramName, out explicitArg) ? explicitArg : Lazy<IDependency_1>) | injector.[Try]Get(typeof(IDependency_1)), ...);
            // }
            //

            ParameterExpression
                injector     = Expression.Parameter(typeof(IInjector), nameof(injector)),
                explicitArgs = Expression.Parameter(typeof(IReadOnlyDictionary<string, object?>), nameof(explicitArgs)),
                explicitArg  = Expression.Variable(typeof(object), nameof(explicitArg));

            return CreateActivator<Func<IInjector, IReadOnlyDictionary<string, object?>, object>>
            (
                constructor,
                ResolveArgument,
                new[] { explicitArg },
                new[]
                {
                    injector,
                    explicitArgs
                }
            );

            Expression ResolveArgument((Type Type, string Name, OptionsAttribute? Options) param)
            {
                //
                // Nem gond ha ez vegul nem interface tipus lesz, az injector.Get() ugy is szol
                // majd miatta.
                //

                Type parameterType = GetEffectiveParameterType(param.Type, out bool isLazy) ?? param.Type;

                //
                // explicitArgs.TryGetValue(paramName, out explicitArg)
                //   ? explicitArg 
                //   : Lazy<IDependency_1>) | injector.[Try]Get(typeof(IDependency_1))
                //

                return Expression.Condition
                (
                    test: Expression.Call
                    (
                        explicitArgs, 
                        DictTryGetValue, 
                        Expression.Constant(param.Name), 
                        explicitArg
                    ),
                    ifTrue: explicitArg,
                    ifFalse: isLazy
                        //
                        // Lazy<IInterface>(() => (IInterface) injector.[Try]Get(typeof(IInterface), svcName))
                        //

                        ? Expression.Invoke
                        (
                            Expression.Constant
                            (
                                GetLazyFactory(parameterType, param.Options)
                            ),
                            injector
                        )

                        //
                        // injector.[Try]Get(typeof(IInterface), svcName)
                        //

                        : Expression.Call
                        (
                            injector,
                            param.Options?.Optional == true ? InjectorTryGet : InjectorGet,
                            Expression.Constant(parameterType),
                            Expression.Constant(param.Options?.Name, typeof(string))
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