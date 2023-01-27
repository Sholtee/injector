/********************************************************************************
* ScopeOptions.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Specifies the scope behavior.
    /// </summary>
    public record ScopeOptions
    {
        /// <summary>
        /// Instructs the scope system to throw if 
        /// <list type="bullet">
        /// <item>A service being requested has a dependency that should live shorter than the service should (e.g.: a singleton service cannot have transient dependency)</item>
        /// <item>The scope (<see cref="IInjector"/>) itself is being requested. This restriction ensures that the dependency graph will not change in runtime.</item>
        /// </list>
        /// </summary>
        public bool StrictDI { get; set; }

        /// <summary>
        /// Specifies whether the created scopes should implement the <see cref="IServiceProvider"/> interface.
        /// </summary>
        /// <remarks>Setting this property to true configures the <see cref="IInjector.Get(Type, string?)"/> method to not throw if the requested dependency cannot be found.</remarks>
        public bool SupportsServiceProvider { get; set; }

        /// <summary>
        /// Specifies the service resolution mode.
        /// </summary>
        public ServiceResolutionMode ServiceResolutionMode { get; set; } = ServiceResolutionMode.AOT;

        /// <summary>
        /// The default options.
        /// </summary>
        public static ScopeOptions Default { get; } = new();
    }
}
