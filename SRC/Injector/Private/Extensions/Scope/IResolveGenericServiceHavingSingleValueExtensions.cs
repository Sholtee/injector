/********************************************************************************
* IResolveGenericServiceHavingSingleValueExtensions.cs                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;

using static System.Diagnostics.Debug;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal static class IResolveGenericServiceHavingSingleValueExtensions
    {
        public static object ResolveGenericServiceHavingSingleValue<TDescendant>(this IResolveGenericServiceHavingSingleValue<TDescendant> self, int slot, Type iface, AbstractServiceEntry openEntry) where TDescendant: IResolveGenericServiceHavingSingleValue<TDescendant>
        {
            Assert(openEntry.Interface.IsGenericTypeDefinition, "Entry must reference an open generic service");
            Assert(iface.IsConstructedGenericType, "The service interface must be a constructed generic type");

            //
            // In case of shared entries retrieve the value from the parent.
            //

            if (openEntry.Flags.HasFlag(ServiceEntryFlags.Shared) && self.Super is not null)
                return self.Super.ResolveGenericServiceHavingSingleValue(slot, iface, openEntry);

            ref Node<Type, object>? node = ref self.GetSlot(slot);

            while (node is not null)
            {
                if (node.Key == iface)
                    return node.Value;
                node = ref node.Next;
            }

            //
            // If the lock already taken, don't enter again (it would have performance penalty)
            //

            bool releaseLock = !Monitor.IsEntered(self.Lock);
            if (releaseLock)
                Monitor.Enter(self.Lock);

            try
            {
                //
                // Another thread may set the node while we reached here
                //

                #pragma warning disable CA1508
                while (node is not null)
                #pragma warning restore CA1508
                {
                    if (node.Key == iface)
                        return node.Value;

                    node = ref node.Next;
                }

                //
                // Writing a "ref" variable is atomic
                //

                node = new Node<Type, object>
                (
                    iface,
                    self.CreateInstance(openEntry.Specialize(iface))
                );

                return node.Value;
            }
            finally
            {
                if (releaseLock)
                    Monitor.Exit(self.Lock);
            }
        }
    }
}
