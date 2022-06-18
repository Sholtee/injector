/********************************************************************************
* ScopedServiceResolverBase.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal abstract class ScopedServiceResolverBase : ServiceResolverBase
    {
        protected static readonly MethodInfo
            FGetOrCreateInstance = MethodInfoExtractor.Extract<IInstanceFactory>(fact => fact.GetOrCreateInstance(null!, 0));

        protected readonly int FSlot;

        protected ScopedServiceResolverBase(AbstractServiceEntry relatedEntry, int slot) : base(relatedEntry) => FSlot = slot;
    }
}
