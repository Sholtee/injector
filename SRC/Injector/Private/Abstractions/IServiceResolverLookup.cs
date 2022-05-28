/********************************************************************************
* IServiceResolverLookup.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI.Internals
{
#if DEBUG // test methods must be public -> make internal types public so they can be passed as parameter
    #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public
#else
    internal
#endif
    interface IServiceResolverLookup
    {
        [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords")]
        Func<IInstanceFactory, object>? Get(Type iface, string? name);

        int Slots { get; }
    }
}
