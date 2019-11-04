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
    public sealed class QueryOptionsAttribute: Attribute
    {
        /// <summary>
        /// The name of the service.
        /// </summary>
        public string Name { get; set; }
    }
}
