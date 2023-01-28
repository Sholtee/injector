/********************************************************************************
* FactoryResolverBase.cs                                                        *
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

    internal abstract class FactoryResolverBase
    {
        private static readonly MethodInfo
            FInjectorGet     = MethodInfoExtractor.Extract<IInjector>(i => i.Get(null!, null)),
            FInjectorTryGet  = MethodInfoExtractor.Extract<IInjector>(i => i.TryGet(null!, null)),
            FCreateLazy      = MethodInfoExtractor.Extract(() => CreateLazy<object>(null!, null)).GetGenericMethodDefinition(),
            FCreateLazyOpt   = MethodInfoExtractor.Extract(() => CreateLazyOpt<object>(null!, null)).GetGenericMethodDefinition();

        private static Lazy<TService> CreateLazy<TService>(IInjector injector, string? name) => new Lazy<TService>(() => (TService)injector.Get(typeof(TService), name));

        private static Lazy<TService> CreateLazyOpt<TService>(IInjector injector, string? name) => new Lazy<TService>(() => (TService)injector.TryGet(typeof(TService), name)!);

        protected static Type? GetEffectiveType(Type type, out bool isLazy)
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

        protected static void EnsureCanBeInstantiated(Type type) 
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
        protected static Expression New
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
        protected static Expression DefaultServiceResolver(ParameterExpression injector, Type type, string _, OptionsAttribute? options)
        {
            type = GetEffectiveType(type, out bool isLazy) ?? throw new ArgumentException(Resources.INVALID_DEPENDENCY);

            return isLazy
                //
                // Lazy<IInterface>(() => (IInterface) injector.[Try]Get(typeof(IInterface), svcName))
                //

                ? ResolveLazyService(injector, type, options)

                //
                // (TInterface) injector.[Try]Get(typeof(IInterface), svcName)
                //

                : ResolveService(injector, type, options);
        }

        protected static Expression<TDelegate> CreateActivator<TDelegate>(Func<IReadOnlyList<ParameterExpression>, Expression> createInstance, params ParameterExpression[] variables) where TDelegate : Delegate
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

        protected static Expression<TDelegate> CreateActivator<TDelegate>
        (
            ConstructorInfo constructor,
            Func<ParameterExpression, Type, string, OptionsAttribute?, Expression> resolveSvc,
            params ParameterExpression[] variables
        ) where TDelegate : Delegate => CreateActivator<TDelegate>
        (
            paramz => New
            (
                constructor,
                paramz.Single(static param => param.Type == typeof(IInjector)),
                resolveSvc
            ),
            variables
        );

        protected static Expression ResolveService(ParameterExpression injector, Type iface, OptionsAttribute? options) => Expression.Convert
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

        protected internal static Expression ResolveLazyService(ParameterExpression injector, Type iface, OptionsAttribute? options)
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
    }
}