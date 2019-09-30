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

        public static Func<IInjector, object> Get(ConstructorInfo constructor) => Cache<ConstructorInfo, Func<IInjector, object>>.GetOrAdd(constructor, () =>
        {
            //
            // injector => new Service(IDependency_1 | Lazy<IDependency_1>, IDependency_2 | Lazy<IDependency_2>,...)
            //

            ParameterExpression injector = Expression.Parameter(typeof(IInjector), nameof(injector));

            Type target = constructor.DeclaringType;

            return constructor.ToLambda<Func<IInjector, object>>
            (
                (param, i) =>
                {
                    Type parameterType = GetParameterType(param, out var isLazy);

                    if (parameterType == null)
                        throw new ArgumentException(Resources.INVALID_CONSTRUCTOR, nameof(constructor));

                    return isLazy
                        //
                        // Lazy<IInterface>(() => (IInterface) injector.Get(typeof(IInterface), target))
                        //

                        ? (Expression) Expression.Invoke(Expression.Constant(GetLazyFactory(parameterType)), injector, Expression.Constant(target))

                        //
                        // injector.Get(typeof(IInterface), target)
                        //

                        : (Expression) Expression.Call(injector, InjectorGet, Expression.Constant(parameterType), Expression.Constant(target));
                },
                injector
            ).Compile();
        });

        public static Func<IInjector, object> Get(Type type) => Cache<Type, Func<IInjector, object>>.GetOrAdd(type, () => Get(type.GetApplicableConstructor()));

        private static Func<IInjector, Type, object> ConvertToFactory(Func<IInjector, object> fn) => (injector, type) => fn(injector);

        //
        // Az GetAsFactory() fv-ek azert vannak gyorsitotarazva h ket szervizbejegyzes azonos parameterekkel
        // azonos hash kodot is adjon vissza.
        //

        public static Func<IInjector, Type, object> GetAsFactory(Type type) => Cache<Type, Func<IInjector, Type, object>>.GetOrAdd(type, () => ConvertToFactory(Get(type)));

        public static Func<IInjector, object> Get(Lazy<Type> type)
        {    
            //
            // A Lazy<> csak azert kell h minden egyes factory hivasnal ne forduljunk a 
            // gyorsitotarhoz.
            //

            var factory = new Lazy<Func<IInjector, object>>(() => Get(type.Value));

            return injector => factory.Value(injector);
        }

        //
        // TODO: FIXME: vhogy gyorsitotarazni
        //

        public static Func<IInjector, Type, object> GetAsFactory(Lazy<Type> type) => ConvertToFactory(Get(type));

        public static Func<IInjector, IReadOnlyDictionary<string, object>, object> GetExtended(ConstructorInfo constructor) => Cache<ConstructorInfo, Func<IInjector, IReadOnlyDictionary<string, object>, object>>.GetOrAdd(constructor, () =>
        {
            //
            // (injector, explicitParamz) => new Service((IDependency_1) (explicitParamz[paramName] ||  injector.Get(typeof(IDependency_1), target)), ...)
            //

            ParameterExpression
                injector     = Expression.Parameter(typeof(IInjector), nameof(injector)),
                explicitArgs = Expression.Parameter(typeof(IReadOnlyDictionary<string, object>), nameof(explicitArgs));

            Type target = constructor.DeclaringType;

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

                Type parameterType = GetParameterType(param, out var isLazy);
                if (parameterType == null)
                    throw new ArgumentException(Resources.INVALID_CONSTRUCTOR_ARGUMENT);

                return isLazy
                    //
                    // Lazy<IInterface>(() => (IInterface) injector.Get(typeof(IInterface), target))
                    //

                    ? GetLazyFactory(parameterType)(injectorInst, target)

                    //
                    // injector.Get(typeof(IInterface), target)
                    //

                    : injectorInst.Get(parameterType, target);
            }
        });

        public static Func<IInjector, IReadOnlyDictionary<string, object>, object> GetExtended(Type type) => Cache<Type, Func<IInjector, IReadOnlyDictionary<string, object>, object>>.GetOrAdd(type, () => GetExtended(type.GetApplicableConstructor()));

        public static Func<IInjector, Type, object> GetLazyFactory(Type iface) => Cache<Type, Func<IInjector, Type, object>>.GetOrAdd(iface, () =>
        {
            Debug.Assert(iface.IsInterface());

            Type delegateType = typeof(Func<>).MakeGenericType(iface);

            //
            // (injector, target) => () => (iface) injector.Get(iface, target)
            //

            ParameterExpression 
                injector = Expression.Parameter(typeof(IInjector), nameof(injector)),
                target   = Expression.Parameter(typeof(Type), nameof(target));

            Func<IInjector, Type, Delegate> createValueFactory = Expression.Lambda<Func<IInjector, Type, Delegate>>
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
                            target
                        ),
                        iface
                    )
                ),
                injector,
                target
            ).Compile();

            //
            // (injector, target) => new Lazy<iface>(Func<iface>)
            //

            ConstructorInfo ctor = typeof(Lazy<>)
                .MakeGenericType(iface)
                .GetConstructor(new []{ delegateType });

            Debug.Assert(ctor != null);

            return (i, t) => ctor.Call(createValueFactory(i, t));
        });
    }
}
