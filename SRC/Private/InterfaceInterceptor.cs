/********************************************************************************
* InterfaceInterceptor.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Provides a mechanism for interceptiong interface method calls.
    /// </summary>
    /// <typeparam name="TInterface">The interface to be intercepted.</typeparam>
    public abstract class InterfaceInterceptor<TInterface> where TInterface: class
    {
        /// <summary>
        /// Internal, don't use it!
        /// </summary>
        public static object CALL_TARGET = new object();

        /// <summary>
        /// The target of this interceptor.
        /// </summary>
        public TInterface Target { get; }

        public InterfaceInterceptor(TInterface target) => Target = target;

        /// <summary>
        /// Called on proxy method invocation.
        /// </summary>
        /// <param name="method">The <see cref="TInterface"/> method that was called</param>
        /// <param name="args">The arguments passed to the method.</param>
        /// <returns>The object to return to the caller, or null for void methods.</returns>
        public virtual object Invoke(MethodInfo method, object[] args) => CALL_TARGET;
    }
}
