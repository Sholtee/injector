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

    using static Properties.Resources;

    internal static partial class ServiceActivator
    {
        private static Expression ResolveDependency(ParameterExpression injector, DependencyDescriptor dependency, object? userData, IReadOnlyList<IDependencyResolver> resolvers)
        {
            return Resolve(0, null);

            Expression Resolve(int i, object? context) => i == resolvers.Count
                ? throw new ArgumentException(INVALID_DEPENDENCY, dependency.Name)
                : resolvers[i].Resolve(injector, dependency, userData, context, context => Resolve(i + 1, context));
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
        private static Expression ResolveService(ConstructorInfo constructor, ParameterExpression injector, object? userData, IReadOnlyList<IDependencyResolver> resolvers) => Expression.MemberInit
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
                            userData,
                            resolvers
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
                            userData,
                            resolvers
                        )
                    )
                )
        );

        private static Expression<TDelegate> CreateActivator<TDelegate>(Func<IReadOnlyList<ParameterExpression>, Expression> createInstance, params ParameterExpression[] variables) where TDelegate : Delegate
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

        private static Expression<TDelegate> CreateActivator<TDelegate>(ConstructorInfo constructor, object? userData, IReadOnlyList<IDependencyResolver> resolvers, params ParameterExpression[] variables) where TDelegate: Delegate
            => CreateActivator<TDelegate>
            (
                paramz => ResolveService
                (
                    constructor,
                    paramz.Single(static param => param.Type == typeof(IInjector)),
                    userData,
                    resolvers
                ),
                variables
            );
    }
}