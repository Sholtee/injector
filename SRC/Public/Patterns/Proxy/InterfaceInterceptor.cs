/********************************************************************************
* InterfaceInterceptor.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;

namespace Solti.Utils.DI.Proxy
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
        /// <param name="method">The <typeparamref name="TInterface"/> method that was called</param>
        /// <param name="args">The arguments passed to the method.</param>
        /// <param name="extra">Extra info about the member from which the <paramref name="method"/> was extracted.</param>
        /// <returns>The object to return to the caller, or null for void methods.</returns>
        /// <remarks>The invocation will be forwarded to the <see cref="Target"/> if this method returns <see cref="CALL_TARGET"/>.</remarks>
        public virtual object Invoke(MethodInfo method, object[] args, MemberInfo extra) => CALL_TARGET;
    }
}
