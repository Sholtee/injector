/********************************************************************************
* ScopedServiceResolverBase.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal abstract class ScopedServiceResolverBase : ServiceResolverBase
    {
        protected readonly int FSlot;

        protected ScopedServiceResolverBase(AbstractServiceEntry relatedEntry, int slot) : base(relatedEntry) => FSlot = slot;
    }
}
