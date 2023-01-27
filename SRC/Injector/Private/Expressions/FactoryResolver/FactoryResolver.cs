/********************************************************************************
* FactoryResolver.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Properties;

    internal class FactoryResolver: FactoryResolverBase
    {
        private static readonly MethodInfo
            FDictTryGetValue = MethodInfoExtractor.Extract<IReadOnlyDictionary<string, object?>, object?>((dict, outVal) => dict.TryGetValue(default!, out outVal));

        private FactoryResolver() { } // class cannot be instantiated (required as static classes cannot have ancestor)

        /// <summary>
        /// <code>(injector, iface) => (object) new Service(IDependency_1 | Lazy&lt;IDependency_1&gt;, IDependency_2 | Lazy&lt;IDependency_2&gt;,...)</code>
        /// </summary>
        public static Expression<FactoryDelegate> Resolve(ConstructorInfo constructor) => CreateActivator<FactoryDelegate>
        (
            constructor, 
            DefaultServiceResolver
        );

        /// <summary>
        /// <code>(injector, iface) => (object) new Service(IDependency_1 | Lazy&lt;IDependency_1&gt;, IDependency_2 | Lazy&lt;IDependency_2&gt;,...)</code>
        /// </summary>
        public static Expression<FactoryDelegate> Resolve(Type type)
        {
            //
            // Itt validaljunk ne a hivo oldalon (kodduplikalas elkerulese vegett).
            //

            EnsureCanBeInstantiated(type);

            return Resolve(type.GetApplicableConstructor());
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
        public static Expression<FactoryDelegate> Resolve(ConstructorInfo constructor, IReadOnlyDictionary<string, object?> explicitArgs)
        {
            ParameterExpression arg = Expression.Variable(typeof(object), nameof(arg));

            return CreateActivator<FactoryDelegate>
            (
                constructor,
                ServiceResolver,
                arg
            );

            Expression ServiceResolver(ParameterExpression injector, Type type, string name, OptionsAttribute? options)
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

                        ? ResolveLazyService(injector, effectiveType, options)

                        //
                        // (TInterface) injector.[Try]Get(typeof(IInterface), svcName)
                        //

                        : ResolveService(injector, type, options)
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
        public static Expression<FactoryDelegate> Resolve(Type type, IReadOnlyDictionary<string, object?> explicitArgs)
        {
            //
            // Itt validaljunk ne a hivo oldalon (kodduplikalas elkerulese vegett).
            //

            EnsureCanBeInstantiated(type);

            return Resolve(type.GetApplicableConstructor(), explicitArgs);
        }

        /// <summary>
        /// <code>
        /// (injector, explicitArgs) => (object) new Service(((ParamzProvider) explicitArgs).argName_1 | Lazy&lt;IDependency_1&gt;() | injector.[Try]Get(typeof(IDependency_1)), ...);
        /// </code>
        /// </summary>
        public static Expression<FactoryDelegate> Resolve(ConstructorInfo constructor, object paramzProvider)
        {
            Type paramzProviderType = paramzProvider.GetType();

            return CreateActivator<FactoryDelegate>
            (
                constructor,
                ServiceResolver
            );

            Expression ServiceResolver(ParameterExpression injector, Type type, string name, OptionsAttribute? options)
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

                    ? ResolveLazyService(injector, type, options)

                    //
                    // (TInterfcae) injector.[Try]Get(typeof(IInterface), svcName)
                    //

                    : ResolveService(injector, type, options);
            }
        }

        /// <summary>
        /// <code>
        /// (injector, explicitArgs) => (object) new Service(((ParamzProvider) explicitArgs).argName_1 | Lazy&lt;IDependency_1&gt;() | injector.[Try]Get(typeof(IDependency_1)), ...);
        /// </code>
        /// </summary>
        public static Expression<FactoryDelegate> Resolve(Type type, object paramzProvider)
        {
            EnsureCanBeInstantiated(type);
            return Resolve(type.GetApplicableConstructor(), paramzProvider);
        }
    }
}