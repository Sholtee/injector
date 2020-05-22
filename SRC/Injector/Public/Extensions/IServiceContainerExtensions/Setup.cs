/********************************************************************************
* Setup.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Solti.Utils.DI
{
    using Interfaces;
    using Internals;

    public static partial class IServiceContainerExtensions
    {
        private static readonly HashSet<char> ReservedChars = new HashSet<char> {'\\', '^', '$', '.', '|', '?', '*', '+', '(', ')', '[', '{'};

        /// <summary>
        /// Registers the annotated services from the given <see cref="Assembly"/>.
        /// </summary>
        /// <param name="container">The target <see cref="IServiceContainer"/>.</param>
        /// <param name="assembly">The source <see cref="Assembly"/>.</param>
        /// <param name="namespace">The namespace in which the search will take place. Note that wildcards (*) are supported.</param>
        /// <returns>The container itself.</returns>
        /// <remarks>You can annotate services with the <see cref="ServiceRegistrationAttribute"/> descendants.</remarks>
        public static IServiceContainer Setup(this IServiceContainer container, Assembly assembly, string @namespace = "*")
        {
            Ensure.Parameter.IsNotNull(container, nameof(container));
            Ensure.Parameter.IsNotNull(assembly, nameof(assembly));
            Ensure.Parameter.IsNotNull(@namespace, nameof(@namespace));

            string pattern = string.Concat(@namespace.Select(chr => 
            {
                if (chr == '*') return ".*";
                if (ReservedChars.Contains(chr)) return $"\\{chr}";
                return $"{chr}"; // NE chr.ToString() legyen h ne dumaljon a fordito
            }));

            var matcher = new Regex($"^{pattern}$");

            foreach (TypeInfo ti in assembly.DefinedTypes.Where(t => (t.IsClass || t.IsInterface) && matcher.IsMatch(t.Namespace ?? string.Empty)))
                foreach (ServiceRegistrationAttribute attr in ti.GetCustomAttributes<ServiceRegistrationAttribute>())
                {
                    IServiceRegistration registration = (IServiceRegistration) typeof(IServiceContainerExtensions)
                        .Assembly
                        .GetType(attr.Implementation, throwOnError: true)
                        .CreateInstance(Array.Empty<Type>());

                    registration.Invoke(container, attr, ti.AsType());
                }

            return container;
        }
    }
}
