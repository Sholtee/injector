/********************************************************************************
* ConfigFromAssembly.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq;
using System.Reflection;

namespace Solti.Utils.DI
{
    using Annotations;

    /// <summary>
    /// Provides the mechanism of <see cref="IServiceContainer"/> "auto-config".
    /// </summary>
    public static class ConfigFromAssembly
    {
        /// <summary>
        /// Registers the annotated services from the given <see cref="Assembly"/>.
        /// </summary>
        /// <param name="target">The target <see cref="IInjector"/>.</param>
        /// <param name="assembly">The source <see cref="Assembly"/>.</param>
        /// <returns>The injector itself.</returns>
        /// <remarks>You can annotate services with the <see cref="ServiceAttribute"/> attribute.</remarks>
        public static IServiceContainer Setup(this IServiceContainer target, Assembly assembly)
        {
            foreach (TypeInfo service in assembly.DefinedTypes.Where(t => t.IsClass))
                foreach (ServiceAttribute serviceAttribute in service.GetCustomAttributes<ServiceAttribute>())
                    target.Service(serviceAttribute.Interface, service.AsType(), serviceAttribute.Lifetime);

            return target;
        }
    }
}
