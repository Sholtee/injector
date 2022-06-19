/********************************************************************************
* IInjectorDiagnosticsExtensions.cs                                             *
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
    public static class IInjectorDiagnosticsExtensions
    {
        /// <summary>
        /// Gets the dependency graph in <a href="https://graphviz.org/">DOT graph</a> format.
        /// </summary>
        public static string GetDependencyGraph(this IInjector injector!!, Type iface!!, string? name = null)
        {
            if (injector is not IServiceEntryLookup serviceEntryLookup)
                throw new NotSupportedException();

            throw new NotImplementedException();
        }
    }
}
