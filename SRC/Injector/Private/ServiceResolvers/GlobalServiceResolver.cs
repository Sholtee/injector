/********************************************************************************
* GlobalServiceResolver.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed class GlobalServiceResolver : ServiceResolverBase
    {
        public GlobalServiceResolver(AbstractServiceEntry relatedEntry) : base(relatedEntry)
        {
        }

        public override object Resolve(IInstanceFactory instanceFactory) => (instanceFactory.Super ?? instanceFactory).CreateInstance(FRelatedEntry);
    }
}
