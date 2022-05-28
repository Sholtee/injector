/********************************************************************************
* IServiceResolver.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

#if DEBUG // test methods must be public -> make internal types public so they can be passed as parameter
    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public
#else
    internal
#endif
    interface IServiceResolver
    {
        AbstractServiceEntry RelatedEntry { get; }

        object Resolve(IInstanceFactory instanceFactory);
    }
}
