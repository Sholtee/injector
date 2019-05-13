/********************************************************************************
* DependencyType.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI
{
    public enum DependencyType
    {
        /// <summary>
        /// Transient lifetime.
        /// </summary>
        /// <remarks>
        /// - Services having transient lifetime are created each time they are requested.
        /// - The caller is responsible for freeing the requested services.
        /// </remarks>
        Transient = 0,

        /// <summary>
        /// Singleton lifetime.
        /// </summary>
        /// <remarks>
        /// - Services having singleton lifetime are created only once they are requested.
        /// - The system automatically frees them on IInjector.Dispose() call.
        /// </remarks>
        Singleton,

        /// <summary>
        /// Internal, don't use.
        /// </summary>
        __InstantiatedSingleton,

        /// <summary>
        /// Internal, don't use.
        /// </summary>
        __Self
    }
}