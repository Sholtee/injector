/********************************************************************************
* GlobalScopedServiceResolver.cs                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed partial class GlobalScopedServiceResolver : ScopedServiceResolverBase
    {
        public GlobalScopedServiceResolver(AbstractServiceEntry relatedEntry, int slot) : base(relatedEntry, slot)
        {
        }

        public override object Resolve(IInstanceFactory instanceFactory) => (instanceFactory.Super ?? instanceFactory).GetOrCreateInstance(FRelatedEntry, FSlot);
    }
}
