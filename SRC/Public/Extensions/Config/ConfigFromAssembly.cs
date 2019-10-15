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
        /// <param name="container">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="assembly">The source <see cref="Assembly"/>.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You can annotate services with the <see cref="ServiceRegistrationAttribute"/> descendants.</remarks>
        public static IServiceContainer Setup(this IServiceContainer container, Assembly assembly)
        {
            foreach (TypeInfo ti in assembly.DefinedTypes)
                foreach (ServiceRegistrationAttribute attr in ti.GetCustomAttributes<ServiceRegistrationAttribute>())
                    attr.Register(container, ti.AsType());

            return container;
        }
    }
}
