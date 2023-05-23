/********************************************************************************
* IInterceptorAggregator.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Proxy;

    internal interface IInterceptorAggregator
    {
        object? CallTarget(InvocationContext ctx);

        object? Target { get; }

        IReadOnlyList<IInterfaceInterceptor> Interceptors { get; }
    }
}
