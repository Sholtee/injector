/********************************************************************************
* IResolveServiceHavingSingleValue.cs                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Threading;

using static System.Diagnostics.Debug;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal static class IResolveServiceHavingSingleValueExtensions
    {
        public static object ResolveServiceHavingSingleValue<TDescendant>(this IResolveServiceHavingSingleValue<TDescendant> self, int slot, AbstractServiceEntry entry) where TDescendant: IResolveServiceHavingSingleValue<TDescendant>
        {
            Assert(entry.Interface.IsGenericTypeDefinition, "Entry must reference a NON generic service");

            //
            // In case of shared entries retrieve the value from the parent.
            //

            if (entry.Flags.HasFlag(ServiceEntryFlags.Shared) && self.Super is not null)
                return self.Super.ResolveServiceHavingSingleValue(slot, entry);

            //------------------------Singleton/Scoped------------------------------------

            ref object? value = ref self.GetSlot(slot);
            if (value is not null)
                return value;

            //----------------------------------------------------------------------------

            //
            // If the lock already taken, don't enter again (it would have performance penalty)
            //

            bool releaseLock = !Monitor.IsEntered(self.Lock);
            if (releaseLock)
                Monitor.Enter(self.Lock);

            try
            {
                //------------------------Singleton/Scoped------------------------------------

                //
                // Another thread may set the value while we reached here
                //

                #pragma warning disable CA1508 // Since we are in a multi-threaded environment this check is required 
                if (value is null)
                #pragma warning restore CA1508
                //-----------------------------------------------------------------------------
                    value = self.CreateInstance(entry);

                return value;
            }
            finally
            {
                if (releaseLock)
                    Monitor.Exit(self.Lock);
            }
        }
    }
}
