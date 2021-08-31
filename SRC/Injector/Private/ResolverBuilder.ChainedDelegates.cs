/********************************************************************************
* ResolverBuilder.ChainedDelegates.cs                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal abstract partial class ResolverBuilder
    {
        private sealed class ChainedDelegatesResolverBuilder: ResolverBuilder
        {
            public override Resolver Build(IEnumerable<AbstractServiceEntry> entries, ResolverCaseBuilder regularEntryResolverBuilder, ResolverCaseBuilder genericEntryResolverBuilder, out int reCount, out int geCount, CancellationToken cancellation = default)
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
                            //
                            // NE tipusok GUID property-ere teszteljunk mert az kibaszott lassu (lasd teljesitmeny tesztek)
                            //

                            Type pureType = iface.IsGenericType ? iface.GetGenericTypeDefinition() : iface;

                            if (entry.Interface == pureType && entry.Name == name)
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
        }
    }
}
