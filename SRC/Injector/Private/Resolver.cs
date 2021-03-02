/********************************************************************************
* Resolver.cs                                                                   *
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

    internal static class Resolver
    {
        private static readonly MethodInfo
            //
            // Csak kifejezesek, nem tenyleges metodus hivas
            //

            InjectorGet = MethodInfoExtractor.Extract<IInjector>(i => i.Get(null!, null)),
            InjectorTryGet = MethodInfoExtractor.Extract<IInjector>(i => i.TryGet(null!, null));

        private static Type? GetParameterType(Type paramType, out bool isLazy)
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

        private static TDelegate CreateResolver<TDelegate>(ConstructorInfo constructor, Func<(Type Type, string Name, OptionsAttribute? Options), Expression> argumentResolver, params ParameterExpression[] parameters)
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

            return Expression.Lambda<TDelegate>
            (
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
                ),
                parameters
            ).Compile();
        }
        public static Func<IInjector, Type, object> Get(ConstructorInfo constructor) => Cache.GetOrAdd(constructor, () =>
        {
            //
            // (injector, iface)  => (object) new Service(IDependency_1 | Lazy<IDependency_1>, IDependency_2 | Lazy<IDependency_2>,...)
            //

            ParameterExpression 
                injector = Expression.Parameter(typeof(IInjector), nameof(injector)),
                iface    = Expression.Parameter(typeof(Type),      nameof(iface));

            return CreateResolver<Func<IInjector, Type, object>>
            (
                constructor, 
                ResolveArgument, 
                injector, 
                iface
            );

            Expression ResolveArgument((Type Type, string _, OptionsAttribute? Options) param) 
            {
                Type parameterType = GetParameterType(param.Type, out bool isLazy) ?? throw new ArgumentException(Resources.INVALID_CONSTRUCTOR, nameof(constructor));

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
            // (injector, explicitParamz) => new Service((IDependency_1) (explicitParamz[paramName] ||  injector.Get(typeof(IDependency_1))), ...)
            //

            ParameterExpression
                injector     = Expression.Parameter(typeof(IInjector), nameof(injector)),
                explicitArgs = Expression.Parameter(typeof(IReadOnlyDictionary<string, object?>), nameof(explicitArgs));

            return CreateResolver<Func<IInjector, IReadOnlyDictionary<string, object?>, object>>
            (
                constructor,
                ResolveArgument,
                injector,
                explicitArgs
            );

            Expression ResolveArgument((Type Type, string Name, OptionsAttribute? Options) param)
            {
                return Expression.Invoke
                (
                    Expression.Constant
                    (
                        (Func<IInjector, IReadOnlyDictionary<string, object?>, object?>) Implementation
                    ),
                    injector,
                    explicitArgs
                );

                object? Implementation(IInjector injectorInst, IReadOnlyDictionary<string, object?> explicitArgsInst)
                {
                    if (explicitArgsInst.TryGetValue(param.Name, out object? value)) 
                        return value;

                    //
                    // Parameter tipust itt KELL validalni h az "explicitArgs"-ban tetszoleges tipusu argumentum
                    // megadhato legyen.
                    //

                    Type? parameterType = GetParameterType(param.Type, out bool isLazy);
                    
                    if (parameterType is null)
                    {
                        var ex = new ArgumentException(Resources.INVALID_CONSTRUCTOR_ARGUMENT);
                        ex.Data["parameter"] = param.Name;

                        throw ex;
                    }

                    //
                    // Lazy<IInterface>(() => (IInterface) injector.Get(typeof(IInterface), svcName))
                    //

                    if (isLazy) return GetLazyFactory(parameterType, param.Options)
                        .Invoke(injectorInst);

                    //
                    // injector.Get(typeof(IInterface), svcName)
                    //

                    return param.Options?.Optional == true
                        ? injectorInst.TryGet(parameterType, param.Options?.Name)
                        : injectorInst.Get(parameterType, param.Options?.Name);
                }
            }
        })!; // Enelkul a CI elszall CS8619-el (helyben nem tudtam reprodukalni)

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
            // injector => () => (iface) injector.[Try]Get(iface, svcName)
            //

            ParameterExpression injector = Expression.Parameter(typeof(IInjector), nameof(injector));

            Func<IInjector, Delegate> createValueFactory = Expression.Lambda<Func<IInjector, Delegate>>
            (
                Expression.Lambda
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
                ),
                injector
            ).Compile();

            //
            // injector => new Lazy<iface>(Func<iface>)
            //

            Func<object[], object> lazyFactory = typeof(Lazy<>)
                .MakeGenericType(iface)
                .GetConstructor(new[] { delegateType })
                .ToStaticDelegate();

            return new Func<IInjector, object>(i => lazyFactory.Invoke(new object[] { createValueFactory(i) }));
        });
    }
}