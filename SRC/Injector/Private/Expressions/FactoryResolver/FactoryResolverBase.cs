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

        protected virtual Expression ResolveDependency
        (
            ParameterExpression injector,
            DependencyDescriptor dependency,
            object? userData
        )
        {
            return Resolve(0);

            Expression Resolve(int i) => i == FResolvers.Count
                ? throw new InvalidOperationException(Resources.INVALID_DEPENDENCY)
                : FResolvers[i].Resolve(injector, dependency, userData, () => Resolve(i + 1));
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
        protected virtual Expression ResolveService
        (
            ConstructorInfo constructor,
            ParameterExpression injector,
            object? userData
        ) => Expression.MemberInit
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
                .Where(property => property.GetCustomAttribute<InjectAttribute>() is not null)
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

        protected Expression<TDelegate> CreateActivator<TDelegate>
        (
            ConstructorInfo constructor,
            object? userData,
            params ParameterExpression[] variables
        ) where TDelegate : Delegate => CreateActivator<TDelegate>
        (
            paramz => ResolveService
            (
                constructor,
                paramz.Single(static param => param.Type == typeof(IInjector)),
                userData
            ),
            variables
        );

        protected FactoryResolverBase(IReadOnlyList<IDependencyResolver>? additionalResolvers)
        {
            List<IDependencyResolver> resolvers = new();
            if (additionalResolvers is not null)
            {
                resolvers.AddRange(additionalResolvers);
            }
            resolvers.AddRange(DefaultDependencyResolvers);
            FResolvers = resolvers;
        }

        public static readonly IReadOnlyList<IDependencyResolver> DefaultDependencyResolvers = new IDependencyResolver[]
        {
            ExplicitArgResolver_Dict.Instance,
            ExplicitArgResolver_Obj.Instance,
            LazyDependencyResolver.Instance,
            RegularDependencyResolver.Instance
        };
    }
}