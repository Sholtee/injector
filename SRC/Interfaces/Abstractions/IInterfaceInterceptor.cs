/********************************************************************************
* IInterfaceInterceptor.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes the contract how to implement an interface interceptor.
    /// </summary>
    /// <remarks>
    /// Interceptors are used to hook into method invocations e.g. for logging:
    /// <code>
    /// public class LoggerInterceptor : IInterfaceInterceptor
    /// {
    ///     public LoggerInterceptor(IDependency dependency) {...}
    ///     
    ///     public object Invoke(IInvocationContext context, Next&lt;IInvocationContext, object?&gt; callNext)
    ///     {
    ///         Console.WriteLine(context.InterfaceMethod);
    ///         return callNext(context);
    ///     }
    /// }
    /// ...
    /// ScopeFactory.Create
    /// (
    ///     svcs => svcs
    ///         .Service&lt;ISemeInterface, SomeImplementation&gt;()
    ///         .Decorate&lt;LoggerInterceptor&gt;(),
    ///     ...
    /// )
    /// </code>
    /// </remarks>
    public interface IInterfaceInterceptor
    {
        /// <summary>
        /// Contains the interception logic.
        /// </summary>
        /// <param name="context">Context related to the actual method call.</param>
        /// <param name="callNext">Calls the next interceptor in the invocation chain.</param>
        /// <returns>Value to be passed to the caller (or NULL in case of void methods).</returns>
        /// <remarks>
        /// Interceptors may modify the input and output parameters or alter the result itself:
        /// <code>
        /// // Invoking the generated proxy instance will trigger this method
        /// object IInterfaceInterceptor.Invoke(IInvocationContext context, Next&lt;IInvocationContext, object?&gt; callNext)
        /// {
        ///     if (suppressOriginalMethod)
        ///     {
        ///         return something;
        ///         // ref|out parameters can be assigned by setting the corresponding "context.Args[]" item 
        ///     }
        ///     context.Args[0] = someNewVal; // "someNewVal" will be forwarded to the original method 
        ///     return callNext(context); // Let the original method do its work
        /// }
        /// </code>
        /// </remarks>
        object? Invoke(IInvocationContext context, Next<IInvocationContext, object?> callNext);
    }
}