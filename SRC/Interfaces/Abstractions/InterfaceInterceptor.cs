/********************************************************************************
* InterfaceInterceptor.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes the contract how to implement an interface interceptor.
    /// </summary>
    /// <remarks>
    /// Interceptors are used to hook into method invocations e.g. for logging:
    /// <code>
    /// public sealed class LoggerInterceptor : InterfaceInterceptor
    /// {
    ///     public LoggerInterceptor(IDependency dependency, InterfaceInterceptor next) : base(next) {...}
    ///     
    ///     public override object Invoke(IInvocationContext context)
    ///     {
    ///         Console.WriteLine(context.InterfaceMethod);
    ///         return Next.Invoke(context);
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
    public abstract class InterfaceInterceptor
    {
        /// <summary>
        /// Creates a new <see cref="InterfaceInterceptor"/> instance.
        /// </summary>
        /// <param name="next">The next interceptor, passed by the system</param>
        protected InterfaceInterceptor(InterfaceInterceptor next)
            => Next = next ?? throw new ArgumentNullException(nameof(next));

        /// <summary>
        /// Creates a new <see cref="InterfaceInterceptor"/> instance.
        /// </summary>
        protected InterfaceInterceptor() { }

        /// <summary>
        /// Contains the interception logic.
        /// </summary>
        /// <param name="context">Context related to the actual method call.</param>
        /// <returns>Value to be passed to the caller (or NULL in case of void methods).</returns>
        /// <remarks>
        /// Interceptors may modify the input and output parameters or alter the result itself:
        /// <code>
        /// // Invoking the generated proxy instance will trigger this method
        /// public override object Invoke(IInvocationContext context)
        /// {
        ///     if (suppressOriginalMethod)
        ///     {
        ///         return something;
        ///         // ref|out parameters can be assigned by setting the corresponding "context.Args[]" item 
        ///     }
        ///     context.Args[0] = someNewVal; // "someNewVal" will be forwarded to the original method 
        ///     return Next.Invoke(context); // Let the original method do its work
        /// }
        /// </code>
        /// </remarks>
        public abstract object? Invoke(IInvocationContext context);

        /// <summary>
        /// The next interceptor in the invocation chain.
        /// </summary>
        /// <remarks>The value of this property is assigned by the system during the instantiation process.</remarks>
        public InterfaceInterceptor? Next { get; }
    }
}