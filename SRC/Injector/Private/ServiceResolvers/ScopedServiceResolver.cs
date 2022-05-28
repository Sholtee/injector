/********************************************************************************
* ScopedServiceResolver.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class ScopedServiceResolver : ScopedServiceResolverBase
    {
        public ScopedServiceResolver(AbstractServiceEntry relatedEntry, int slot) : base(relatedEntry, slot)
        {
        }

        public override object Resolve(IInstanceFactory instanceFactory) => instanceFactory.GetOrCreateInstance(FRelatedEntry, FSlot);
    }
}
