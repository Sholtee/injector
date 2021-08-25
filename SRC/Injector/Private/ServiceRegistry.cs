/********************************************************************************
* ServiceRegistry.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;
    using Primitives.Patterns;

    public interface IServiceRegistry : IComposite<IServiceRegistry>, INotifyOnDispose
    {
        AbstractServiceEntry? GetEntry(Type iface, string? name);

        IReadOnlyList<AbstractServiceEntry> RegisteredEntries { get; }
    }

    public delegate AbstractServiceEntry? Resolver(ServiceRegistry self, Type iface, string? name);

    public delegate Resolver ResolverBuilder(int index, AbstractServiceEntry entry);

    public interface IResolverBuilder
    {
        Resolver Build(IEnumerable<AbstractServiceEntry> entries, ResolverBuilder regularEntryResolverBuilder, ResolverBuilder genericEntryResolverBuilder, out int reCount, out int geCount);
    }

    public sealed class CompiledExpressionResolverBuilder: IResolverBuilder
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

        public Resolver Build(IEnumerable<AbstractServiceEntry> entries, ResolverBuilder regularEntryResolverBuilder, ResolverBuilder genericEntryResolverBuilder, out int reCount, out int geCount)
        {
            if (entries is null)
                throw new ArgumentNullException(nameof(entries));

            if (regularEntryResolverBuilder is null)
                throw new ArgumentNullException(nameof(regularEntryResolverBuilder));

            if (genericEntryResolverBuilder is null)
                throw new ArgumentNullException(nameof(genericEntryResolverBuilder));

            ParameterExpression
                self  = Expression.Parameter(typeof(ServiceRegistry), nameof(self)),
                iface = Expression.Parameter(typeof(Type), nameof(iface)),
                name  = Expression.Parameter(typeof(string), nameof(name));

            PropertyInfo
                guidProp = (PropertyInfo) ((MemberExpression) ((Expression<Func<Type, Guid>>)(t => t.GUID)).Body).Member,
                parentProp = (PropertyInfo) ((MemberExpression) ((Expression<Func<ServiceRegistry, ServiceRegistry?>>)(sr => sr.Parent)).Body).Member;

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

    public sealed class ChainedMethodsResolverBuilder : IResolverBuilder
    {
        public Resolver Build(IEnumerable<AbstractServiceEntry> entries, ResolverBuilder regularEntryResolverBuilder, ResolverBuilder genericEntryResolverBuilder, out int reCount, out int geCount)
        {
            if (entries is null)
                throw new ArgumentNullException(nameof(entries));

            if (regularEntryResolverBuilder is null)
                throw new ArgumentNullException(nameof(regularEntryResolverBuilder));

            if (genericEntryResolverBuilder is null)
                throw new ArgumentNullException(nameof(genericEntryResolverBuilder));

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

                    resolver = (self, iface, name) =>
                    {
                        if (entry.Interface == iface && entry.Name == name)
                        {
                            return reResolver(self, iface, name);
                        }
                        return next(self, iface, name);
                    };
                }

                if (entry.IsShared)
                {
                    Resolver baseResolver = resolver;

                    resolver = (self, iface, name) => self.Parent is not null
                        ? self.Parent.GetEntry(iface, name)
                        : baseResolver(self, iface, name);
                }
            }

            reCount = regularEntryCount;
            geCount = genericEntryCount;

            return resolver;
        }

        public override string ToString() => nameof(ChainedMethodsResolverBuilder);
    }

    public class ServiceRegistry : Composite<IServiceRegistry>, IServiceRegistry
    {
        #region Private
        private readonly Resolver FResolver;

        private readonly EntryHolder[] FRegularEntries;

        private readonly ConcurrentDictionary<Type, Lazy<AbstractServiceEntry>>[] FSpecializedEntries;

        private readonly ConcurrentCollection<AbstractServiceEntry> FUsedEntries = new(); // Add() lehet hivva parhuzamosan

        private static T[] CreateArray<T>(Func<T> factory, int count)
        {
            T[] result = new T[count];

            for (int i = 0; i < count; i++)
            {
                result[i] = factory();
            }

            return result;
        }

        private sealed class EntryHolder
        {
            public AbstractServiceEntry? Value;
        }
        #endregion

        #region Protected
        protected virtual Resolver RegularEntryResolverFactory(int index, AbstractServiceEntry entry) => (self, iface, name) =>
        {
            EntryHolder holder = self.FRegularEntries[index];

            if (holder.Value is null)
            {
                lock (holder) // entry csak egyszer legyen masolva
                {
                    if (holder.Value is null)
                    {
                        holder.Value = entry.Copy();
                        self.FUsedEntries.Add(holder.Value);
                    }
                }
            }

            return holder.Value;
        };

        protected virtual Resolver GenericEntryResolverFactory(int index, AbstractServiceEntry entry)
        {
            if (entry is not ISupportsSpecialization supportsSpecialization)
                throw new InvalidOperationException(); // TODO

            return (self, iface, name) =>
            {
                ConcurrentDictionary<Type, Lazy<AbstractServiceEntry>> specializedEntries = self.FSpecializedEntries[index];

                //
                // Lazy azert kell mert ha ugyanarra a kulcsra parhuzamosan kerul meghivasra a GetOrAdd() akkor a factory
                // tobbszor is meg lehet hivva (lasd MSDN).
                //

                return specializedEntries
                    .GetOrAdd(iface, _ => new Lazy<AbstractServiceEntry>(Specialize, LazyThreadSafetyMode.ExecutionAndPublication))
                    .Value;

                AbstractServiceEntry Specialize()
                {
                    AbstractServiceEntry specializedEntry = supportsSpecialization.Specialize(iface.GenericTypeArguments);

                    self.FUsedEntries.Add(specializedEntry);
                    return specializedEntry;
                }
            };
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                foreach (AbstractServiceEntry usedEntry in FUsedEntries)
                {
                    usedEntry.Dispose();
                }
            }
            base.Dispose(disposeManaged);
        }

        protected async override ValueTask AsyncDispose()
        {
            await Task.WhenAll
            (
                FUsedEntries.Select(usedEntry => usedEntry.DisposeAsync().AsTask())
            );

            await base.AsyncDispose();
        }
        #endregion

        public ServiceRegistry(IEnumerable<AbstractServiceEntry> entries, IResolverBuilder resolverBuilder): base()
        {
            if (entries is null)
                throw new ArgumentNullException(nameof(entries));

            if (resolverBuilder is null)
                throw new ArgumentNullException(nameof(resolverBuilder));

            RegisteredEntries = entries.ToArray(); // elsonek legyen inicialva [BuildResolver() hasznalja]

            FResolver = resolverBuilder.Build(RegisteredEntries, RegularEntryResolverFactory, GenericEntryResolverFactory, out int reCount, out int geCount);
            FRegularEntries = CreateArray(() => new EntryHolder(), reCount);
            FSpecializedEntries = CreateArray(() => new ConcurrentDictionary<Type, Lazy<AbstractServiceEntry>>(), geCount);
        }

        public ServiceRegistry(ServiceRegistry parent): base(parent)
        {
            if (parent is null)
                throw new ArgumentNullException(nameof(parent));

            RegisteredEntries = parent.RegisteredEntries;

            FResolver = parent.FResolver;
            FRegularEntries = CreateArray(() => new EntryHolder(), parent.FRegularEntries.Length);
            FSpecializedEntries = CreateArray(() => new ConcurrentDictionary<Type, Lazy<AbstractServiceEntry>>(), parent.FSpecializedEntries.Length);
        }

        public new ServiceRegistry? Parent => (ServiceRegistry?) base.Parent;

        public AbstractServiceEntry? GetEntry(Type iface, string? name) => FResolver.Invoke(this, iface ?? throw new ArgumentNullException(nameof(iface)), name);

        public IReadOnlyList<AbstractServiceEntry> RegisteredEntries { get; }
    }
}
