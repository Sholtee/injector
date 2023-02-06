/********************************************************************************
* IInterfaceInterceptor.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes the contract how to implement an interface method call interceptor.
    /// </summary>
    public interface IInterfaceInterceptor
    {
        /// <summary>
        /// Contains the interception logic.
        /// </summary>
        object? Invoke(IInvocationContext context, Next<object?> callNext);
    }
}