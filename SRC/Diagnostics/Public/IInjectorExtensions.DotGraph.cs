/********************************************************************************
* IInjectorExtensions.DotGraph.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Diagnostics
{
    using Interfaces;
    using Internals;

    public static partial class IInjectorExtensions
    {
        /// <summary>
        /// Gets the dependency graph in <a href="https://graphviz.org/">DOT graph</a> format.
        /// </summary>
        public static string GetDependencyGraph(this IInjector injector, Type serviceInterface, string? serviceName = null)
        {
            if (injector is null)
                throw new ArgumentNullException(nameof(injector));

            return injector
                .GetReference(serviceInterface, serviceName)
                .AsDotGraph()
                .ToString();
        }

        /// <summary>
        /// Gets the dependency graph in <a href="https://graphviz.org/">DOT graph</a> format.
        /// </summary>
        public static string GetDependencyGraph<TInterface>(this IInjector injector, string? serviceName = null) where TInterface : class =>
            injector.GetDependencyGraph(typeof(TInterface), serviceName);
    }
}
