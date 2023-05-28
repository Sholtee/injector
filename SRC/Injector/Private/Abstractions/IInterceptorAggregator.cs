/********************************************************************************
* IInterceptorAggregator.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Proxy;

    internal interface IInterceptorAggregator
    {
        object? CallTarget(InvocationContext ctx);

        object? Target { get; }

        IInterfaceInterceptor[] Interceptors { get; }
    }
}
