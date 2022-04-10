/********************************************************************************
* IResolveGenericServiceExtensions.cs                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;

using static System.Diagnostics.Debug;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal static class IResolveGenericServiceExtensions
    {
        public static object ResolveGenericService<TDescendant>(this IResolveGenericService<TDescendant> self, int slot, Type iface, AbstractServiceEntry openEntry) where TDescendant: IResolveGenericService<TDescendant>
        {
            Assert(openEntry.Interface.IsGenericTypeDefinition, "Entry must reference an open generic service");
            Assert(!openEntry.Flags.HasFlag(ServiceEntryFlags.CreateSingleInstance), "Entry must be allowed to resolve multiple values");
            Assert(iface.IsConstructedGenericType, "The service interface must be a constructed generic type");

            if (openEntry.Flags.HasFlag(ServiceEntryFlags.Shared) && self.Super is not null)
                return self.Super.ResolveGenericService(slot, iface, openEntry);

            ref Node<Type, AbstractServiceEntry>? node = ref self.GetSlot(slot);

            while (node is not null && node.Key != iface)
            {
                node = ref node.Next;
            }

            bool releaseLock = !Monitor.IsEntered(self.Lock);
            if (releaseLock)
                Monitor.Enter(self.Lock);

            try
            {
                //
                // Another thread may set the node while we reached here.
                //

                while (node is not null && node.Key != iface)
                {
                    node = ref node.Next;
                }

                if (node is null)
                    node = new Node<Type, AbstractServiceEntry>(iface, openEntry.Specialize(iface));

                return self.CreateInstance(node.Value);
            }
            finally
            {
                if (releaseLock)
                    Monitor.Exit(self.Lock);
            }
        }
    }
}
