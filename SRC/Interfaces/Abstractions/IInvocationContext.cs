/********************************************************************************
* IInvocationContext.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Contains the related context of a perticular method invocation
    /// </summary>
    /// <remarks>This context is used during interface interception in <see cref="IInterfaceInterceptor.Invoke(IInvocationContext, Next{IInvocationContext, object?})"/> method.</remarks>
    public interface IInvocationContext
    {
        /// <summary>
        /// The arguments passed by the caller.
        /// </summary>
        /// <remarks>
        /// You can modify input arguments by changening the corrensponing value in this array before dispatching the invocation:
        /// <code>
        /// object IInterfaceInterceptor.Invoke(IInvocationContext context, Next&lt;object&gt; callNext)
        /// {
        ///     context.Args[0] = "cica";
        ///     return callNext();
        /// }
        /// </code>
        /// </remarks>
        object?[] Args { get; }

        /// <summary>
        /// The underlying proxy instance. It's safe to cast it to the actual interface.
        /// </summary>
        /// <remarks>
        /// This property is useful when you want to invoke interface members involving their interceptors too:
        /// <code>
        /// object IInterfaceInterceptor.Invoke(IInvocationContext context, Next&lt;object&gt; callNext)
        /// {
        ///     ((IMyService) context.ProxyInstance).SomeMethod();
        ///     return callNext();
        /// }
        /// </code>
        /// </remarks>
        object ProxyInstance { get; }

        /// <summary>
        /// The concrete method behind the <see cref="InterfaceMember"/>.
        /// </summary>
        MethodInfo InterfaceMethod { get; }

        /// <summary>
        /// The member (property, event or method) that is being invoked.
        /// </summary> 
        MemberInfo InterfaceMember { get; }

        /// <summary>
        /// The concrete method behind the <see cref="TargetMember"/>.
        /// </summary>  
        MethodInfo TargetMethod { get; }

        /// <summary>
        /// The member (property, event or method) that is being targeted.
        /// </summary>    
        MemberInfo TargetMember { get; }

        /// <summary>
        /// Context belonging to the next interceptor.
        /// </summary>
        IInvocationContext? Next { get; }

        /// <summary>
        /// Invokes the actual interceptor. Meant for private use only.
        /// </summary>
        /// <returns></returns>
        object? InvokeInterceptor();
    }
}