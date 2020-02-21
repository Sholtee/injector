/********************************************************************************
* QueryOptionsAttribute.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Annotations
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
        /// <remarks>This option is ignored if you are using the MS preferred DI (<see cref="IServiceProvider"/>).</remarks>
        public string Name { get; set; }

        /// <summary>
        /// Indicates whether the service is optional or not.
        /// </summary>
        /// <remarks>This option is ignored if you are using the MS preferred DI (<see cref="IServiceProvider"/>).</remarks>
        public bool Optional { get; set; }
    }
}
