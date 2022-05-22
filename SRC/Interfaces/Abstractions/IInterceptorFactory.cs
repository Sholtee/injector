/********************************************************************************
* IInterceptorFactory.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes the contract how to create interface interceptors. Implementors are supposed to be aspects.
    /// </summary>
    /// <remarks>At the moment "TInterceptor" should be either <see cref="Type"/> or Expression&lt;Func&lt;<see cref="IInjector"/>, <see cref="Type"/>, <see cref="object"/>, <see cref="object"/>&gt;&gt;.</remarks>
    public interface IInterceptorFactory<TInterceptor>
    {
        /// <summary>
        /// Returns the interceptor instance for the given <paramref name="iface"/>.
        /// </summary>
        TInterceptor GetInterceptor(Type iface);
    }
}
