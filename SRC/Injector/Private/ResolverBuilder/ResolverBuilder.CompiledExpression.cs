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
            //   Type pureType = iface.IsGenericType
            //     ? iface.GetGenericTypeDefinition()
            //     : iface;
            //
            //   switch (pureType) // NE iface.GUID property-re vizsgaljunk mert kibaszott lassu lekeredzni
            //   {
            //     case typeof(IServiceA):
            //     {
            //       switch (name)
            //       {
            //         case null: return GetLocalEntryA(self);
            //         ...
            //         default: return null;
            //       }
            //     }
            //     case typeof(IServiceB): // IsShared
            //     {
            //        case "cica": return self.Parent is not null
            //            ? self.Parent.GetEntry(iface, name)
            //            : GetLocalEntryB(self);
            //         ...
            //         default: return null;
            //     }
            //     case typeof(IServiceC<>):
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
                    self = Expression.Parameter(typeof(IServiceRegistry), nameof(self)),
                    iface = Expression.Parameter(typeof(Type), nameof(iface)),
                    name = Expression.Parameter(typeof(string), nameof(name)),
                    pureType = Expression.Variable(typeof(Type), nameof(pureType));

                PropertyInfo
                    parentProp = ExtractProperty<IServiceRegistry, IServiceRegistry?>(r => r.Parent),
                    isGeneric = ExtractProperty<Type, bool>(t => t.IsGenericType);

                MethodInfo
                    getEntryMethod = ExtractMethod<IServiceRegistry>(r => r.GetEntry(null!, null)),
                    getGenericTypeMethod = ExtractMethod<Type>(t => t.GetGenericTypeDefinition());

                int // GetEntryResolver()-ben nem hivatkozhatunk by-ref parametert
                    regularEntryCount = 0,
                    genericEntryCount = 0;

                LabelTarget returnLabel = Expression.Label(typeof(AbstractServiceEntry));

                Expression<Resolver> lambda = Expression.Lambda<Resolver>
                (
                    Expression.Block
                    (
                        type: typeof(AbstractServiceEntry),
                        variables: new[] { pureType },
                        Expression.Assign
                        (
                            left: pureType, 
                            right: Expression.Condition
                            (
                                test: Expression.Property(iface, isGeneric),
                                ifTrue: Expression.Call(iface, getGenericTypeMethod),
                                ifFalse: iface
                            )
                        ),
                        CreateSwitch
                        (
                            value: pureType,
                            cases: entries
                                .GroupBy(entry => entry.Interface)
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

                static PropertyInfo ExtractProperty<TTarget, TProp>(Expression<Func<TTarget, TProp>> expr) => (PropertyInfo) ((MemberExpression) expr.Body).Member;

                static MethodInfo ExtractMethod<TTarget>(Expression<Action<TTarget>> expr) => ((MethodCallExpression) expr.Body).Method;
            }
        }
    }
}
