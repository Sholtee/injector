/********************************************************************************
* ResolverBuilder.CompiledExpression.cs                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;

    internal abstract partial class ResolverBuilder
    {
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

            public override Resolver Build(IEnumerable<AbstractServiceEntry> entries, ResolverCaseBuilder regularEntryResolverBuilder, ResolverCaseBuilder genericEntryResolverBuilder, out int reCount, out int geCount, CancellationToken cancellation = default)
            {
                Ensure.Parameter.IsNotNull(entries, nameof(entries));
                Ensure.Parameter.IsNotNull(regularEntryResolverBuilder, nameof(regularEntryResolverBuilder));
                Ensure.Parameter.IsNotNull(genericEntryResolverBuilder, nameof(genericEntryResolverBuilder));

                ParameterExpression
                    self  = Expression.Parameter(typeof(IServiceRegistry), nameof(self)),
                    iface = Expression.Parameter(typeof(Type), nameof(iface)),
                    name  = Expression.Parameter(typeof(string), nameof(name));

                PropertyInfo
                    guidProp   = (PropertyInfo) ((MemberExpression) ((Expression<Func<Type, Guid>>) (t => t.GUID)).Body).Member,
                    parentProp = (PropertyInfo) ((MemberExpression) ((Expression<Func<IServiceRegistry, IServiceRegistry?>>) (sr => sr.Parent)).Body).Member;

                MethodInfo
                    getEntryMethod = ((MethodCallExpression) ((Expression<Action<IServiceRegistry>>) (sr => sr.GetEntry(null!, null))).Body).Method;

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
                            test: Expression.NotEqual(root, Expression.Constant(null, typeof(IServiceRegistry))),
                            ifTrue: Expression.Call(root, getEntryMethod, iface, name),
                            ifFalse: invocation
                        );
                    }

                    return Expression.Return(returnLabel, invocation);
                }
            }
        }
    }
}
