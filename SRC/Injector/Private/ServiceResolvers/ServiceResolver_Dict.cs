﻿/********************************************************************************
* ServiceResolver_Dict.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Interfaces.Properties;

    internal sealed class ServiceResolver_Dict: ServiceResolverBase
    {
        private sealed record CompositeKey(long Handle, string? Name);

        private readonly IReadOnlyDictionary<CompositeKey, AbstractServiceEntry> FGenericEntries;

        //
        // Don't use ImmutableArray here since it is 2-3 times slower.
        //

        private volatile /*IReadOnly*/Dictionary<CompositeKey, Func<IInstanceFactory, object>> FResolvers;

        public const string Id = "dict";

        public ServiceResolver_Dict(IEnumerable<AbstractServiceEntry> entries)
        {
            Dictionary<CompositeKey, AbstractServiceEntry> genericEntries = new();
            Dictionary<CompositeKey, Func<IInstanceFactory, object>> resolvers = new();

            foreach (AbstractServiceEntry entry in entries)
            {
                CompositeKey key = new((long) entry.Interface.TypeHandle.Value, entry.Name);
                if (entry.Interface.IsGenericTypeDefinition)
                    genericEntries.Add(key, entry);
                else
                {
#if NETSTANDARD2_1_OR_GREATER
                    if (!resolvers.TryAdd(key, CreateResolver(entry)))
#else
                    try
                    {
                        resolvers.Add(key, CreateResolver(entry));
                    }
                    catch (ArgumentException)
#endif
                    {
                        InvalidOperationException ex = new(Resources.SERVICE_ALREADY_REGISTERED);
                        ex.Data[nameof(entry)] = entry;
                        throw ex;
                    }
                }
            }

            FGenericEntries = genericEntries;
            FResolvers = resolvers;
        }

        public override Func<IInstanceFactory, object>? Get(Type iface, string? name)
        {
            CompositeKey key = new((long) iface.TypeHandle.Value, name);

            if (!FResolvers.TryGetValue(key, out Func<IInstanceFactory, object> resolver) && iface.IsConstructedGenericType && FGenericEntries.TryGetValue(new CompositeKey((long) iface.GetGenericTypeDefinition().TypeHandle.Value, name), out AbstractServiceEntry genericEntry))
            {
                lock (FLock)
                {
                    //
                    // FResolvers instance may be changed while we reached here
                    //

                    if (!FResolvers.TryGetValue(key, out resolver))
                    {
                        //
                        // Create a new resolver for the specializted entry
                        //

                        resolver = CreateResolver
                        (
                            genericEntry.Specialize(iface.GenericTypeArguments)
                        );

                        //
                        // In theory copying a dictionary is quick: https://github.com/dotnet/runtime/blob/c78bf2f522b4ce5a449faf6a38a0752b642a7f79/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/Dictionary.cs#L126
                        //

                        Dictionary<CompositeKey, Func<IInstanceFactory, object>> extendedResolvers = new(FResolvers);
                        extendedResolvers.Add(key, resolver);

                        FResolvers = extendedResolvers;
                    }
                }
            }

            return resolver;
        }
    }
}
