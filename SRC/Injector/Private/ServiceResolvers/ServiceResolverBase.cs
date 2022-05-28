/********************************************************************************
* ServiceResolverBase.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal abstract class ServiceResolverBase: IServiceResolver
    {
        protected readonly AbstractServiceEntry FRelatedEntry;

        AbstractServiceEntry IServiceResolver.RelatedEntry => FRelatedEntry;

        protected ServiceResolverBase(AbstractServiceEntry relatedEntry) => FRelatedEntry = relatedEntry;

        public abstract object Resolve(IInstanceFactory instanceFactory);
    }
}
