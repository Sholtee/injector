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
    using Internals;

    public static partial class IServiceContainerExtensions
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
            Ensure.Parameter.IsNotNull(container, nameof(container));
            Ensure.Parameter.IsNotNull(assembly, nameof(assembly));

            foreach (TypeInfo ti in assembly.DefinedTypes.Where(t => t.IsClass || t.IsInterface))
                foreach (ServiceRegistrationAttribute attr in ti.GetCustomAttributes<ServiceRegistrationAttribute>())
                    attr.Register(container, ti.AsType());

            return container;
        }
    }
}
