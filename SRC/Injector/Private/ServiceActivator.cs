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
            Ensure.Parameter.IsNotNull(type, nameof(type));
            Ensure.Parameter.IsClass(type, nameof(type));
            Ensure.Parameter.IsNotAbstract(type, nameof(type));
            Ensure.Parameter.IsNotGenericDefinition(type, nameof(type));
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
                        ),
                        typeof(object)
                    )
                ),
                injector,
                iface,
                additinalParameter
            );

            Debug.WriteLine($"Created activator:{Environment.NewLine}{resolver.GetDebugView()}");

            return resolver;
        }

        public static Expression<Func<IInjector, Type, object>> Get(ConstructorInfo constructor)
        {
            //
            // (injector, iface)  => (object) new Service(IDependency_1 | Lazy<IDependency_1>, IDependency_2 | Lazy<IDependency_2>,...)
            //

            return CreateActivator<Func<IInjector, Type, object>>
            (
                constructor, 
                ResolveDependency,
                null
            );

            static Expression ResolveDependency(ParameterExpression injector, Type type, string __, OptionsAttribute? options) 
            {
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

        public static Expression<Func<IInjector, Type, object, object>> GetLateBound(ConstructorInfo constructor, int argIndex)
        {
            int i = 0;
            ParameterExpression explicitArg = Expression.Parameter(typeof(object), nameof(explicitArg));

            //
            // (injector, objects)  => (object) new Service(explicit | IDependency_1 | Lazy<IDependency_1>, explicit | IDependency_2 | Lazy<IDependency_2>,...)
            //

            return CreateActivator<Func<IInjector, Type, object, object>>
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

        public static Expression<Func<IInjector, Type, object>> Get(Type type)
        {
            //
            // Itt validaljunk ne a hivo oldalon (kodduplikalas elkerulese vegett).
            //

            EnsureCanBeInstantiated(type);

            return Get(type.GetApplicableConstructor());
        }

        public static Expression<Func<IInjector, Type, object>> Get(ConstructorInfo constructor, IReadOnlyDictionary<string, object?> explicitArgs)
        {
            ParameterExpression arg = Expression.Variable(typeof(object), nameof(arg));

            //
            // (injector, explicitArgs) =>
            // {
            //    object arg;
            //    return new Service(explicitArgs.TryGetValue(paramName, out arg) ? arg : Lazy<IDependency_1>) | injector.[Try]Get(typeof(IDependency_1)), ...);
            // }
            //

            return CreateActivator<Func<IInjector, Type, object>>
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

        public static Expression<Func<IInjector, Type, object>> Get(Type type, IReadOnlyDictionary<string, object?> explicitArgs)
        {
            //
            // Itt validaljunk ne a hivo oldalon (kodduplikalas elkerulese vegett).
            //

            EnsureCanBeInstantiated(type);

            return Get(type.GetApplicableConstructor(), explicitArgs);
        }

        public static Expression<Func<IInjector, Type, object>> Get(ConstructorInfo constructor, object paramzProvider)
        {
            Type paramzProviderType = paramzProvider.GetType();

            //
            // (injector, explicitArgs) =>
            //    return (object) new Service(((ParamzProvider) explicitArgs).argName_1 | Lazy<IDependency_1>() | injector.[Try]Get(typeof(IDependency_1)), ...);
            //

            return CreateActivator<Func<IInjector, Type, object>>
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

        public static Expression<Func<IInjector, Type, object>> Get(Type type, object paramzProvider)
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
            Ensure.Parameter.IsInterface(iface, nameof(iface));
            Ensure.Parameter.IsNotGenericDefinition(iface, nameof(iface));

            //
            // According to ServiceActivator_Lazy perf tests, the runtime built lambdas are by-design ridiculously slow
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
    }
}