/********************************************************************************
* ServiceResolver.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class ServiceResolver : ServiceResolverBase
    {
        public ServiceResolver(AbstractServiceEntry relatedEntry) : base(relatedEntry)
        {
        }

        public override object Resolve(IInstanceFactory instanceFactory) => instanceFactory.CreateInstance(FRelatedEntry);
    }
}
