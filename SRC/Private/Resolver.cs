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
    using Annotations;
    using Properties;
    using System.Threading;

    internal static class Resolver
    {
        private static readonly MethodInfo InjectorGet = ((MethodCallExpression) ((Expression<Action<IInjector>>) (i => i.Get(null, null))).Body).Method;

        private static Type GetParameterType(ParameterInfo param, out bool isLazy)
        {
            Type parameterType = param.ParameterType;

            if (parameterType.IsInterface())
            {
                isLazy = false;
                return parameterType;
            }

            if (parameterType.IsGenericType() && parameterType.GetGenericTypeDefinition() == typeof(Lazy<>))
            {
                parameterType = parameterType.GetGenericArguments().Single();
                if (parameterType.IsInterface())
                {
                    isLazy = true;
                    return parameterType;
                }
            }

            isLazy = false;
            return null;
        }

        public static Func<IInjector, Type, object> Get(ConstructorInfo constructor) => Cache<ConstructorInfo, Func<IInjector, Type, object>>.GetOrAdd(constructor, () =>
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

                    string svcName = param.GetCustomAttribute<OptionsAttribute>()?.Name;

                    return isLazy
                        //
                        // Lazy<IInterface>(() => (IInterface) injector.Get(typeof(IInterface), svcName))
                        //

                        ? (Expression) Expression.Invoke(Expression.Constant(GetLazyFactory(parameterType, svcName)), injector)

                        //
                        // injector.Get(typeof(IInterface), svcName)
                        //

                        : (Expression) Expression.Call(injector, InjectorGet, Expression.Constant(parameterType), Expression.Constant(svcName /*lehet NULL*/, typeof(string)));
                },
                injector,
                iface
            ).Compile();
        });

        public static Func<IInjector, Type, object> Get(Type type) => Cache<Type, Func<IInjector, Type, object>>.GetOrAdd(type, () => Get(type.GetApplicableConstructor()));

        //
        // Igaz itt nincs idoigenyes operacio ami miatt gyorsitotarazni kene viszont a ServiceEntry-k
        // egyezossegenek vizsgalatahoz kell.
        //

        public static Func<IInjector, Type, object> Get(Lazy<Type> type) => Cache<Lazy<Type>, Func<IInjector, Type, object>>.GetOrAdd(type, () =>
        {    
            //
            // A Lazy<> csak azert kell h minden egyes factory hivasnal ne forduljunk a 
            // gyorsitotarhoz.
            //

            var factory = new Lazy<Func<IInjector, Type, object>>(() => Get(type.Value), LazyThreadSafetyMode.ExecutionAndPublication);

            return (injector, iface) => factory.Value(injector, iface);
        });

        public static Func<IInjector, IReadOnlyDictionary<string, object>, object> GetExtended(ConstructorInfo constructor) => Cache<ConstructorInfo, Func<IInjector, IReadOnlyDictionary<string, object>, object>>.GetOrAdd(constructor, () =>
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

            object GetArg(ParameterInfo param, IInjector injectorInst, IReadOnlyDictionary<string, object> explicitArgsInst)
            {
                if (explicitArgsInst.TryGetValue(param.Name, out var value)) return value;

                //
                // Parameter tipust itt KELL validalni h az "explicitArgs"-ban tetszoleges tipusu argumentum
                // megadhato legyen.
                //

                Type parameterType = GetParameterType(param, out var isLazy) ?? throw new ArgumentException(Resources.INVALID_CONSTRUCTOR_ARGUMENT);

                string svcName = param.GetCustomAttribute<OptionsAttribute>()?.Name;

                return isLazy
                    //
                    // Lazy<IInterface>(() => (IInterface) injector.Get(typeof(IInterface), svcName))
                    //

                    ? GetLazyFactory(parameterType, svcName)(injectorInst)

                    //
                    // injector.Get(typeof(IInterface), svcName)
                    //

                    : injectorInst.Get(parameterType, svcName);
            }
        });

        public static Func<IInjector, IReadOnlyDictionary<string, object>, object> GetExtended(Type type) => Cache<Type, Func<IInjector, IReadOnlyDictionary<string, object>, object>>.GetOrAdd(type, () => GetExtended(type.GetApplicableConstructor()));

        public static Func<IInjector, object> GetLazyFactory(Type iface, string svcName) => Cache<(Type Interface, string Name), Func<IInjector, object>>.GetOrAdd((iface, svcName), () =>
        {
            Debug.Assert(iface.IsInterface());

            Type delegateType = typeof(Func<>).MakeGenericType(iface);

            //
            // injector => () => (iface) injector.Get(iface, svcName)
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
                            InjectorGet,
                            Expression.Constant(iface),
                            Expression.Constant(svcName /*lehet NULL*/, typeof(string))
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

            return i => ctor.Call(createValueFactory(i));
        });
    }
}
