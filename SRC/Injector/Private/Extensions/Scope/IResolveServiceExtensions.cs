/********************************************************************************
* IResolveServiceExtensions.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Threading;

using static System.Diagnostics.Debug;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal static class IResolveServiceExtensions
    {
        public static object ResolveService<TDescendant>(this IResolveService<TDescendant> self, AbstractServiceEntry entry) where TDescendant: IResolveService<TDescendant>
        {
            Assert(entry.Interface.IsGenericTypeDefinition, "Entry must reference a NON generic service");

            if (entry.Flags.HasFlag(ServiceEntryFlags.Shared) && self.Super is not null)
                return self.Super.ResolveService(entry);

            bool releaseLock = !Monitor.IsEntered(self.Lock);
            if (releaseLock)
                Monitor.Enter(self.Lock);

            try
            {
                return self.CreateInstance(entry);
            }
            finally
            {
                if (releaseLock)
                    Monitor.Exit(self.Lock);
            }
        }
    }
}
