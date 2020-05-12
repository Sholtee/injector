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
    using Properties;

    internal static class Resolver
    {
        private static readonly MethodInfo
            //
            // Csak kifejezesek, nem tenyleges metodus hivas
            //

            InjectorGet = MethodInfoExtractor.Extract<IInjector>(i => i.Get(null!, null)),
            InjectorTryGet = MethodInfoExtractor.Extract<IInjector>(i => i.TryGet(null!, null));

        private static Type? GetParameterType(ParameterInfo param, out bool isLazy)
        {
            Type parameterType = param.ParameterType;

            if (parameterType.IsInterface)
            {
                isLazy = false;
                return parameterType;
            }

            if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(Lazy<>))
            {
                parameterType = parameterType.GetGenericArguments().Single();
                if (parameterType.IsInterface)
                {
                    isLazy = true;
                    return parameterType;
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

        public static Func<IInjector, Type, object> Get(ConstructorInfo constructor) => Cache.GetOrAdd(constructor, () =>
        {
            //
            // (injector, iface)  => new Service(IDependency_1 | Lazy<IDependency_1>, IDependency_2 | Lazy<IDependency_2>,...)
            //

            ParameterExpression 
                injector = Expression.Parameter(typeof(IInjector), nameof(injector)),
                iface    = Expression.Parameter(typeof(Type),      nameof(iface));

            return constructor.ToLambda<Func<IInjector, Type, object>>
            (
                (param, i) =>
                {
                    Type parameterType = GetParameterType(param, out var isLazy) ?? throw new ArgumentException(Resources.INVALID_CONSTRUCTOR, nameof(constructor));

                    OptionsAttribute? options = param.GetCustomAttribute<OptionsAttribute>();

                    return isLazy
                        //
                        // Lazy<IInterface>(() => (IInterface) injector.[Try]Get(typeof(IInterface), svcName))
                        //

                        ? (Expression) Expression.Invoke(Expression.Constant(GetLazyFactory(parameterType, options)), injector)

                        //
                        // injector.[Try]Get(typeof(IInterface), svcName)
                        //

                        : (Expression) Expression.Call(
                            injector,
                            options?.Optional == true ? InjectorTryGet : InjectorGet, 
                            Expression.Constant(parameterType), 
                            Expression.Constant(options?.Name, typeof(string)));
                },
                injector,
                iface
            ).Compile();
        });

        public static Func<IInjector, Type, object> Get(Type type)
        {
            //
            // Itt validaljunk ne a hivo oldalon (kodduplikalas elkerulese vegett).
            //

            EnsureCanBeInstantiated(type);

            return Cache.GetOrAdd(type, () => Get(type.GetApplicableConstructor()));
        }

        public static Func<IInjector, IReadOnlyDictionary<string, object>, object> GetExtended(ConstructorInfo constructor) => Cache.GetOrAdd(constructor, () =>
        {
            //
            // (injector, explicitParamz) => new Service((IDependency_1) (explicitParamz[paramName] ||  injector.Get(typeof(IDependency_1))), ...)
            //

            ParameterExpression
                injector     = Expression.Parameter(typeof(IInjector), nameof(injector)),
                explicitArgs = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), nameof(explicitArgs));

            return constructor.ToLambda<Func<IInjector, IReadOnlyDictionary<string, object>, object>>
            (
                (param, i) => Expression.Invoke(Expression.Constant((Func<ParameterInfo, IInjector, IReadOnlyDictionary<string, object>, object>) GetArg), Expression.Constant(param), injector, explicitArgs),
                injector,
                explicitArgs
            ).Compile();

            static object GetArg(ParameterInfo param, IInjector injectorInst, IReadOnlyDictionary<string, object> explicitArgsInst)
            {
                if (explicitArgsInst.TryGetValue(param.Name, out var value)) return value;

                //
                // Parameter tipust itt KELL validalni h az "explicitArgs"-ban tetszoleges tipusu argumentum
                // megadhato legyen.
                //

                Type parameterType = GetParameterType(param, out var isLazy) ?? throw new ArgumentException(Resources.INVALID_CONSTRUCTOR_ARGUMENT);

                OptionsAttribute? options = param.GetCustomAttribute<OptionsAttribute>();

                return isLazy
                    //
                    // Lazy<IInterface>(() => (IInterface) injector.Get(typeof(IInterface), svcName))
                    //

                    ? GetLazyFactory(parameterType, options).Invoke(injectorInst)

                    //
                    // injector.Get(typeof(IInterface), svcName)
                    //

                    : injectorInst.Get(parameterType, options?.Name);
            }
        })!; // Enelkul a CI elszall CS8619-el (helyben nem tudtam reprodukalni)

        public static Func<IInjector, IReadOnlyDictionary<string, object>, object> GetExtended(Type type) => Cache.GetOrAdd(type, () => 
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

            ConstructorInfo ctor = typeof(Lazy<>)
                .MakeGenericType(iface)
                .GetConstructor(new []{ delegateType });

            Debug.Assert(ctor != null);

            return new Func<IInjector, object>(i => ctor!.Call(createValueFactory(i)));
        });
    }
}
