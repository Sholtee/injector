/********************************************************************************
* ResolverBuilder.ChainedDelegates.cs                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;
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

                reCount = 0;

                foreach (AbstractServiceEntry entry in entries.Where(entry => !entry.Interface.IsGenericTypeDefinition))
                {
                    Resolver 
                        next = resolver,
                        reResolver = regularEntryResolverBuilder(reCount++, entry);

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

                geCount = 0;

                foreach (AbstractServiceEntry entry in entries.Where(entry => entry.Interface.IsGenericTypeDefinition))
                {
                    Resolver 
                        next = resolver,
                        geResolver = genericEntryResolverBuilder(geCount++, entry);

                    if (entry.IsShared)
                        geResolver = InvokeParent(geResolver);

                    resolver = (self, iface, name) =>
                    {
                        if (entry.Interface == iface.GetGenericTypeDefinition() && entry.Name == name)
                        {
                            return geResolver(self, iface, name);
                        }
                        return next(self, iface, name);
                    };
                }

                return resolver;

                static Resolver InvokeParent(Resolver resolver) => (self, iface, name) => self.Parent is not null
                    ? self.Parent.GetEntry(iface, name)
                    : resolver(self, iface, name);
            }
        }
    }
}
