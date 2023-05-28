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
        private readonly IReadOnlyList<IDependencyResolver> FResolvers;

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

        protected virtual Expression ResolveDependency(ParameterExpression injector, DependencyDescriptor dependency, object? userData)
        {
            return Resolve(0, null);

            Expression Resolve(int i, object? context) => i == FResolvers.Count
                ? throw new ArgumentException(Resources.INVALID_DEPENDENCY, dependency.Name)
                : FResolvers[i].Resolve(injector, dependency, userData, context, context => Resolve(i + 1, context));
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
        protected virtual Expression ResolveService(ConstructorInfo constructor, ParameterExpression injector, object? userData) => Expression.MemberInit
        (
            Expression.New
            (
                constructor,
                constructor
                    .GetParameters()
                    .Select
                    (
                        param => ResolveDependency
                        (
                            injector,
                            new DependencyDescriptor(param),
                            userData
                        )
                    )
            ),
            constructor
                .ReflectedType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.FlattenHierarchy)
                .Where(static property => property.GetCustomAttribute<InjectAttribute>() is not null)
                .Select
                (
                    property => Expression.Bind
                    (
                        property,
                        ResolveDependency
                        (
                            injector,
                            new DependencyDescriptor(property),
                            userData
                        )
                    )
                )
        );

        protected static Expression<TDelegate> CreateActivator<TDelegate>(Func<IReadOnlyList<ParameterExpression>, Expression> createInstance, params ParameterExpression[] variables) where TDelegate : Delegate
        {
            MethodInfo invoke = typeof(TDelegate).GetMethod(nameof(Action.Invoke));

            List<ParameterExpression> paramz = new
            (
                invoke
                    .GetParameters()
                    .Select(static param => Expression.Parameter(param.ParameterType, param.Name))
            );

            Expression<TDelegate> resolver = Expression.Lambda<TDelegate>
            (
                Expression.Block
                (
                    variables,
                    Expression.Convert
                    (
                        createInstance(paramz),
                        invoke.ReturnType
                    )
                ),
                paramz
            );

            Debug.WriteLine($"Created activator:{Environment.NewLine}{resolver.GetDebugView()}");

            return resolver;
        }

        protected Expression<TDelegate> CreateActivator<TDelegate>(ConstructorInfo constructor, object? userData, params ParameterExpression[] variables) where TDelegate: Delegate
            => CreateActivator<TDelegate>
            (
                paramz => ResolveService
                (
                    constructor,
                    paramz.Single(static param => param.Type == typeof(IInjector)),
                    userData
                ),
                variables
            );

        protected FactoryResolverBase(IReadOnlyList<IDependencyResolver>? resolvers)
            => FResolvers = resolvers ?? DefaultDependencyResolvers.Value;
    }
}