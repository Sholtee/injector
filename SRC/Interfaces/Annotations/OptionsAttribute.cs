/********************************************************************************
* OptionsAttribute.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Controls the <see cref="IInjector"/> during the dependency resolution.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class OptionsAttribute: Attribute
    {
        /// <summary>
        /// The name of the service.
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        /// Indicates whether the service is optional or not.
        /// </summary>
        /// <remarks>This option is ignored if you are using the MS preferred DI (<see cref="IServiceProvider"/>).</remarks>
        public bool Optional { get; init; }
    }
}
