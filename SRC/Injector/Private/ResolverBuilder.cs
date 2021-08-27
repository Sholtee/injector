/********************************************************************************
* ResolverBuilder.cs                                                            *
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

    /// <summary>
    /// Represents a built service entry resolver.
    /// </summary>
    public delegate AbstractServiceEntry? Resolver(ServiceRegistry self, Type iface, string? name);

    /// <summary>
    /// Represents an abstract resolver builder.
    /// </summary>
    public abstract class ResolverBuilder
    {
        /// <summary>
        /// Represents a built resolver case.
        /// </summary>
        public delegate Resolver ResolverCaseBuilder(int index, AbstractServiceEntry entry);

        /// <summary>
        /// The build logic.
        /// </summary>
        public abstract Resolver Build(IEnumerable<AbstractServiceEntry> entries, ResolverCaseBuilder regularEntryResolverBuilder, ResolverCaseBuilder genericEntryResolverBuilder, out int reCount, out int geCount);

        /// <summary>
        /// Returns a <see cref="ResolverBuilder"/> that uses compiled <see cref="Expression"/>s.
        /// </summary>
        public static ResolverBuilder CompiledExpression { get; } = new CompiledExpressionResolverBuilder();

        /// <summary>
        /// Returns a <see cref="ResolverBuilder"/> that uses chained <see cref="Delegate"/>s.
        /// </summary>
        public static ResolverBuilder ChainedDelegates { get; } = new ChainedDelegatesResolverBuilder();

        #region Private
        private sealed class CompiledExpressionResolverBuilder : ResolverBuilder
        {
            private static Expression CreateSwitch<TKey>(Expression value, IEnumerable<(TKey Key, Expression Body)> cases) => Expression.Switch
            (
                value,
                cases.Select
                (
                    @case => Expression.SwitchCase
                    (
                        @case.Body,
                        Expression.Constant(@case.Key, typeof(TKey))
                    )
                ).ToArray()
            );

            //
            // (self, iface, name) =>
            // {
            //   switch (iface.GUID) // GUID nyilt es lezart generikusnal ugyanaz
            //   {
            //     case typeof(IServiceA).GUID:
            //     {
            //       switch (name)
            //       {
            //         case null: return GetLocalEntryA(self);
            //         ...
            //         default: return null;
            //       }
            //     }
            //     case typeof(IServiceB).GUID: // IsShared
            //     {
            //        case "cica": return self.Parent is not null
            //            ? self.Parent.GetEntry(iface, name)
            //            : GetLocalEntryB(self);
            //         ...
            //         default: return null;
            //     }
            //     case typeof(IServiceC<>).GUID:
            //     {
            //       switch (name)
            //       {
            //         case null: return GetLocalSpecializedEntryC(self, iface);
            //         ...
            //         default: return null;
            //       }
            //     }
            //     ...
            //     default: return null;
            //   }
            // }
            //

            public override Resolver Build(IEnumerable<AbstractServiceEntry> entries, ResolverCaseBuilder regularEntryResolverBuilder, ResolverCaseBuilder genericEntryResolverBuilder, out int reCount, out int geCount)
            {
                Ensure.Parameter.IsNotNull(entries, nameof(entries));
                Ensure.Parameter.IsNotNull(regularEntryResolverBuilder, nameof(regularEntryResolverBuilder));
                Ensure.Parameter.IsNotNull(genericEntryResolverBuilder, nameof(genericEntryResolverBuilder));

                ParameterExpression
                    self = Expression.Parameter(typeof(ServiceRegistry), nameof(self)),
                    iface = Expression.Parameter(typeof(Type), nameof(iface)),
                    name = Expression.Parameter(typeof(string), nameof(name));

                PropertyInfo
                    guidProp = (PropertyInfo) ((MemberExpression) ((Expression<Func<Type, Guid>>) (t => t.GUID)).Body).Member,
                    parentProp = (PropertyInfo) ((MemberExpression) ((Expression<Func<ServiceRegistry, ServiceRegistry?>>) (sr => sr.Parent)).Body).Member;

                MethodInfo
                    getEntryMethod = ((MethodCallExpression) ((Expression<Action<ServiceRegistry>>) (sr => sr.GetEntry(null!, null))).Body).Method;

                int // GetEntryResolver()-ben nem hivatkozhatunk by-ref parametert
                    regularEntryCount = 0,
                    genericEntryCount = 0;

                LabelTarget returnLabel = Expression.Label(typeof(AbstractServiceEntry));

                Expression<Resolver> lambda = Expression.Lambda<Resolver>
                (
                    Expression.Block
                    (
                        type: typeof(AbstractServiceEntry),
                        CreateSwitch
                        (
                            value: Expression.Property(iface, guidProp),
                            cases: entries
                                .GroupBy(entry => entry.Interface.GUID)
                                .Select
                                (
                                    grp =>
                                    (
                                        value: grp.Key,
                                        cases: CreateSwitch
                                        (
                                            name,
                                            grp.Select(entry => (entry.Name, GetEntryResolver(entry)))
                                        )
                                    )
                                )
                        ),
                        Expression.Label(returnLabel, Expression.Default(typeof(AbstractServiceEntry)))
                    ),
                    self, iface, name
                );

                reCount = regularEntryCount;
                geCount = genericEntryCount;

                Debug.WriteLine(lambda.GetDebugView());
                return lambda.Compile();

                Expression GetEntryResolver(AbstractServiceEntry entry)
                {
                    Expression invocation = Expression.Invoke
                    (
                        Expression.Constant
                        (
                            entry.Interface.IsGenericTypeDefinition
                                ? genericEntryResolverBuilder(genericEntryCount++, entry)
                                : regularEntryResolverBuilder(regularEntryCount++, entry)
                        ),
                        self, iface, name
                    );

                    if (entry.IsShared)
                    {
                        MemberExpression root = Expression.Property(self, parentProp);

                        invocation = Expression.Condition
                        (
                            test: Expression.NotEqual(root, Expression.Constant(null, typeof(ServiceRegistry))),
                            ifTrue: Expression.Call(root, getEntryMethod, iface, name),
                            ifFalse: invocation
                        );
                    }

                    return Expression.Return(returnLabel, invocation);
                }
            }

            public override string ToString() => nameof(CompiledExpressionResolverBuilder);
        }

        private sealed class ChainedDelegatesResolverBuilder: ResolverBuilder
        {
            public override Resolver Build(IEnumerable<AbstractServiceEntry> entries, ResolverCaseBuilder regularEntryResolverBuilder, ResolverCaseBuilder genericEntryResolverBuilder, out int reCount, out int geCount)
            {
                Ensure.Parameter.IsNotNull(entries, nameof(entries));
                Ensure.Parameter.IsNotNull(regularEntryResolverBuilder, nameof(regularEntryResolverBuilder));
                Ensure.Parameter.IsNotNull(genericEntryResolverBuilder, nameof(genericEntryResolverBuilder));

                Resolver resolver = (_, _, _) => null;

                int // GetEntryResolver()-ben nem hivatkozhatunk by-ref parametert
                    regularEntryCount = 0,
                    genericEntryCount = 0;

                foreach (AbstractServiceEntry entry in entries)
                {
                    Resolver next = resolver;

                    if (entry.Interface.IsGenericTypeDefinition)
                    {
                        Resolver geResolver = genericEntryResolverBuilder(genericEntryCount++, entry);
                        if (entry.IsShared)
                            geResolver = InvokeParent(geResolver);

                        resolver = (self, iface, name) =>
                        {
                            if (entry.Interface.GUID == iface.GUID && entry.Name == name)
                            {
                                return geResolver(self, iface, name);
                            }
                            return next(self, iface, name);
                        };
                    }
                    else
                    {
                        Resolver reResolver = regularEntryResolverBuilder(regularEntryCount++, entry);
                        if (entry.IsShared)
                            reResolver = InvokeParent(reResolver);

                        resolver = (self, iface, name) =>
                        {
                            if (entry.Interface == iface && entry.Name == name)
                            {
                                return reResolver(self, iface, name);
                            }
                            return next(self, iface, name);
                        };
                    }

                    static Resolver InvokeParent(Resolver resolver) => (self, iface, name) => self.Parent is not null
                        ? self.Parent.GetEntry(iface, name)
                        : resolver(self, iface, name);
                }

                reCount = regularEntryCount;
                geCount = genericEntryCount;

                return resolver;
            }

            public override string ToString() => nameof(ChainedDelegatesResolverBuilder);
        }
        #endregion
    }
}
