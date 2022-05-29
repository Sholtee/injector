/********************************************************************************
* ServiceResolverLookup_BTree.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Interfaces.Properties;

    using Primitives;
    using Primitives.Patterns;

    internal class ServiceResolverLookup_BTree : ServiceResolverLookupBase
    {
        #region Private
        private static readonly StringComparer FStringComparer = StringComparer.Ordinal;

        internal static int CompareServiceIds(long iface1, string? name1, long iface2, string? name2)
        {
            //
            // We have to return Int32 -> Math.Sign()
            //

            int order = Math.Sign(iface1 - iface2);
            if (order is 0)
                //
                // StringComparer supports NULL despite it is not reflected by nullable annotation
                //

                order = FStringComparer.Compare(name1!, name2!);
            return order;
        }

        private interface ICompositeKey
        {
            long Interface { get; }
            string? Name { get; }
        }

        private sealed class NodeComparer : Singleton<NodeComparer>, IComparer<ICompositeKey>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(ICompositeKey x, ICompositeKey y) => CompareServiceIds
            (
                x.Interface, x.Name,
                y.Interface, y.Name
            );
        }

        private static ResolutionNode<TResult> CreateNode<TResult>(AbstractServiceEntry key, TResult value) => new ResolutionNode<TResult>
        (
            (long) key.Interface.TypeHandle.Value,
            key.Name,
            value
        );

        private static TResult? GetResult<TResult>(ResolutionNode<TResult>? node, long iface, string? name)
        {
            if (node is null)
                return default;

            int order = CompareServiceIds
            (
                iface,
                name,
                node.Interface,
                node.Name
            );

            if (order is 0)
                return node.Result;

            if (order < 0)
            {
                if (node.Left is not ResolutionNode<TResult> child)
                    return default;

                return GetResult(child, iface, name);
            }

            if (order > 0)
            {
                if (node.Right is not ResolutionNode<TResult> child)
                    return default;

                return GetResult(child, iface, name);
            }

            return default;
        }
        #endregion

        #region Protected
        protected readonly RedBlackTree<ResolutionNode<AbstractServiceEntry>> FGetGenericEntrySwitch;

        protected volatile RedBlackTree<ResolutionNode<IServiceResolver>> FGetResolverSwitch;

        protected ResolutionNode<IServiceResolver> CreateServiceResolutionNode(AbstractServiceEntry entry) => CreateNode
        (
            entry,
            CreateResolver(entry)
        );

        protected sealed class ResolutionNode<TResult> : RedBlackTreeNode, ICompositeKey
        {
            public ResolutionNode(NodeColor color, long iface, string? name, TResult result) : base(color)
            {
                Interface = iface;
                Name = name;
                Result = result;
            }

            public ResolutionNode(long iface, string? name, TResult result) : this(NodeColor.Unspecified, iface, name, result) { }

            public override RedBlackTreeNode ShallowClone() => new ResolutionNode<TResult>(Color, Interface, Name, Result);

            public long Interface { get; }

            public string? Name { get; }

            public TResult Result { get; }
        }
        #endregion

        public ServiceResolverLookup_BTree(IEnumerable<AbstractServiceEntry> entries)
        {
            FGetGenericEntrySwitch = new RedBlackTree<ResolutionNode<AbstractServiceEntry>>(NodeComparer.Instance);
            FGetResolverSwitch = new RedBlackTree<ResolutionNode<IServiceResolver>>(NodeComparer.Instance);

            foreach (AbstractServiceEntry entry in entries)
            {
                bool added = entry.Interface.IsGenericTypeDefinition
                    ? FGetGenericEntrySwitch.Add
                    (
                        CreateNode(entry, entry)
                    )
                    : FGetResolverSwitch.Add
                    (
                        CreateServiceResolutionNode(entry)
                    );
                if (!added)
                {
                    InvalidOperationException ex = new(Resources.SERVICE_ALREADY_REGISTERED);
                    ex.Data[nameof(entry)] = entry;
                    throw ex;
                }
            }
        }

        public override IServiceResolver? Get(Type iface, string? name)
        {
            long handle = (long) iface.TypeHandle.Value;

            IServiceResolver? resolver = GetResult
            (
                FGetResolverSwitch.Root,
                handle,
                name
            );
            if (resolver is null && iface.IsConstructedGenericType)
            {
                AbstractServiceEntry? genericEntry = GetResult
                (
                    FGetGenericEntrySwitch.Root,
                    (long) iface.GetGenericTypeDefinition().TypeHandle.Value,
                    name
                );
                if (genericEntry is not null)
                {
                    lock (FLock)
                    {
                        //
                        // Another thread might have registered the resolver while we reached here.
                        //

                        resolver = GetResult
                        (
                            FGetResolverSwitch.Root,
                            handle,
                            name
                        );
                        if (resolver is null)
                        {
                            ResolutionNode<IServiceResolver> node = CreateServiceResolutionNode
                            (
                                genericEntry.Specialize(iface.GenericTypeArguments)
                            );

                            FGetResolverSwitch = FGetResolverSwitch.With(node);

                            resolver = node.Result;
                        }
                    }
                }
            }

            return resolver;
        }
    }
}
