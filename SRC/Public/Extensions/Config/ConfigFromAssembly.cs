/********************************************************************************
* ConfigFromAssembly.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.DI
{
    using Annotations;

    /// <summary>
    /// Provides an extension to auto-config the <see cref="IServiceContainer"/>.
    /// </summary>
    public static class ConfigFromAssembly
    {
        /// <summary>
        /// Registers the annotated services from the given assembly.
        /// </summary>
        /// <param name="target">The target injector.</param>
        /// <param name="assembly">The source assembly.</param>
        /// <returns>The injector itself.</returns>
        public static IServiceContainer Setup(this IServiceContainer target, Assembly assembly)
        {
            foreach (Type service in assembly.DefinedTypes.Where(t => t.IsClass))
                foreach (ServiceAttribute serviceAttribute in service.GetCustomAttributes<ServiceAttribute>())
                    target.Service(serviceAttribute.Interface, service, serviceAttribute.Lifetime);

            return target;
        }
    }
}
