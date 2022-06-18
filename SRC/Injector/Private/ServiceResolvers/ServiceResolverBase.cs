/********************************************************************************
* ServiceResolverBase.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal abstract class ServiceResolverBase: IServiceResolver
    {
        protected static readonly MethodInfo
            FCreateInstance = MethodInfoExtractor.Extract<IInstanceFactory>(fact => fact.CreateInstance(null!));

        protected static readonly PropertyInfo
            FSuper = PropertyInfoExtractor.Extract<IInstanceFactory, IInstanceFactory?>(fact => fact.Super);

        protected readonly AbstractServiceEntry FRelatedEntry;

        AbstractServiceEntry IServiceResolver.RelatedEntry => FRelatedEntry;

        protected ServiceResolverBase(AbstractServiceEntry relatedEntry) => FRelatedEntry = relatedEntry;

        public abstract object Resolve(IInstanceFactory instanceFactory);

        public abstract Expression GetResolveExpression(Expression instanceFactory);
    }
}
