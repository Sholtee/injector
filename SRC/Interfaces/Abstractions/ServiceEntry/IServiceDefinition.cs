/********************************************************************************
* IServiceDefinition.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes a service definition.
    /// </summary>
    public interface IServiceDefinition: IServiceId
    {
        /// <summary>
        /// The owner of the service.
        /// </summary>
        IServiceRegistry? Owner { get; }

        /// <summary>
        /// The related (optional) implementation. 
        /// </summary>
        Type? Implementation { get; }

        /// <summary>
        /// The related (optional) factory function.
        /// </summary>
        Func<IInjector, Type, object>? Factory { get; }
    }
}
