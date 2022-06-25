/********************************************************************************
* IScopeFactoryDiagnosticsExtensions.cs                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Diagnostics
{
    using Interfaces;
    using Internals;

    /// <summary>
    /// Defines diagnostics related extensions for the <see cref="IInjector"/> interface.
    /// </summary>
    public static class IScopeFactoryDiagnosticsExtensions
    {
        /// <summary>
        /// Gets the dependency graph in <a href="https://graphviz.org/">DOT graph</a> format.
        /// </summary>
        public static string GetDependencyGraph(this IScopeFactory root!!, Type iface!!, string? name = null, string? newLine = null)
        {
            if (root is not IServiceEntryLookup serviceEntryLookup)
                throw new NotSupportedException();

            DotGraphBuilder graphBuilder = new(serviceEntryLookup);
            graphBuilder.BuildById(iface, name);

            return graphBuilder.Graph.ToString(newLine ?? Environment.NewLine);
        }

        /// <summary>
        /// Gets the dependency graph in <a href="https://graphviz.org/">DOT graph</a> format.
        /// </summary>
        public static string GetDependencyGraph<TService>(this IScopeFactory root!!, string? name = null, string? newLine = null) where TService : class =>
            root.GetDependencyGraph(typeof(TService), name, newLine);
    }
}
