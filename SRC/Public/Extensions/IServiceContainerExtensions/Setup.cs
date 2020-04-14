/********************************************************************************
* Setup.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Solti.Utils.DI
{
    using Internals;

    public static partial class IServiceContainerExtensions
    {
        /// <summary>
        /// Registers the annotated services from the given <see cref="Assembly"/>.
        /// </summary>
        /// <param name="container">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="assembly">The source <see cref="Assembly"/>.</param>
        /// <param name="namespace">The namespace in which the search will take place.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You can annotate services with the <see cref="ServiceRegistrationAttribute"/> descendants.</remarks>
        public static IServiceContainer Setup(this IServiceContainer container, Assembly assembly, string @namespace = "*")
        {
            Ensure.Parameter.IsNotNull(container, nameof(container));
            Ensure.Parameter.IsNotNull(assembly, nameof(assembly));
            Ensure.Parameter.IsNotNull(@namespace, nameof(@namespace));

            string pattern = Regex.Replace(@namespace, "\\.|\\*", match => match.Value switch
            {
                "." => "\\.",
                "*" => "[\\w\\W.]*",
                _ => throw new NotSupportedException()
            });

            var matcher = new Regex($"^{pattern}$");

            foreach (TypeInfo ti in assembly.DefinedTypes.Where(t => (t.IsClass || t.IsInterface) && matcher.IsMatch(t.Namespace ?? string.Empty)))
                foreach (ServiceRegistrationAttribute attr in ti.GetCustomAttributes<ServiceRegistrationAttribute>())
                    attr.Register(container, ti.AsType());

            return container;
        }
    }
}
