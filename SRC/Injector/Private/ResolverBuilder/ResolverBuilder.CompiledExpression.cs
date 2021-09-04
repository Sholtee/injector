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
            //   switch (iface) // NE iface.GUID property-re vizsgaljunk mert kibaszott lassu lekeredzni
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
            //     case typof(IServiceC<int>): // lezart generikus
            //     {
            //       switch (name)
            //       {
            //         case null: return GetLocalEntryC(self);
            //         ...
            //         default: return null;
            //       }     
            //     }
            //  }
            //
            //  if (!iface.IsGenericType)
            //     return null;
            //
            //  switch(iface.GetGenericTypeDefinition())
            //  {
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
                    name = Expression.Parameter(typeof(string), nameof(name));

                PropertyInfo
                    parentProp = PropertyInfoExtractor.Extract<IServiceRegistry, IServiceRegistry?>(r => r.Parent),
                    isGeneric = PropertyInfoExtractor.Extract<Type, bool>(t => t.IsGenericType);

                MethodInfo
                    getEntryMethod = MethodInfoExtractor.Extract<IServiceRegistry>(r => r.GetEntry(null!, null)),
                    getGenericTypeMethod = MethodInfoExtractor.Extract<Type>(t => t.GetGenericTypeDefinition());

                int // GetEntryResolver()-ben nem hivatkozhatunk by-ref parametert
                    regularEntryCount = 0,
                    genericEntryCount = 0;


                IEnumerable<IGrouping<Type, AbstractServiceEntry>>? groupedEntries = entries.GroupBy(entry => entry.Interface);

                LabelTarget returnLabel = Expression.Label(typeof(AbstractServiceEntry));

                Expression 
                    nonGenericSwitch = CreateSwitch
                    (
                        value: iface,
                        cases: groupedEntries
                            .Where(grp => !grp.Key.IsGenericTypeDefinition)
                            .Select
                            (
                                grp =>
                                (
                                    value: grp.Key,
                                    cases: CreateSwitch
                                    (
                                        name,
                                        grp.Select
                                        (
                                            entry => 
                                            (
                                                entry.Name,
                                                GetEntryResolver(entry, regularEntryResolverBuilder, regularEntryCount++)
                                            )
                                        )
                                    )
                                )
                            )
                    ),
                    genericSwitch = CreateSwitch
                    (
                        value: Expression.Call(iface, getGenericTypeMethod),
                        cases: groupedEntries
                            .Where(grp => grp.Key.IsGenericTypeDefinition)
                            .Select
                            (
                                grp =>
                                (
                                    value: grp.Key,
                                    cases: CreateSwitch
                                    (
                                        name,
                                        grp.Select
                                        (
                                            entry =>
                                            (
                                                entry.Name,
                                                GetEntryResolver(entry, genericEntryResolverBuilder, genericEntryCount++)
                                            )
                                        )
                                    )
                                )
                            )
                    );

                Expression<Resolver> lambda = Expression.Lambda<Resolver>
                (
                    Expression.Block
                    (
                        type: typeof(AbstractServiceEntry),
                        nonGenericSwitch,
                        Expression.IfThen
                        (
                            test: Expression.Not(Expression.Property(iface, isGeneric)),
                            ifTrue: Expression.Return(returnLabel, Expression.Default(typeof(AbstractServiceEntry)))
                        ),
                        genericSwitch,
                        Expression.Label(returnLabel, Expression.Default(typeof(AbstractServiceEntry)))
                    ),
                    self, iface, name
                );

                reCount = regularEntryCount;
                geCount = genericEntryCount;

                Debug.WriteLine(lambda.GetDebugView());
                return lambda.Compile();

                Expression GetEntryResolver(AbstractServiceEntry entry, ResolverCaseBuilder caseBuilder, int index)
                {
                    Expression invocation = Expression.Invoke
                    (
                        Expression.Constant
                        (
                            caseBuilder(index, entry)
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
