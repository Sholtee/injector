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
    public sealed record ScopeOptions
    {
        /// <summary>
        /// Instructs the system to throw if 
        /// <list type="bullet">
        /// <item>The service being requested has a dependency that should live shorter than the service should (e.g.: a singleton service cannot have transient dependency)</item>
        /// <item>The scope (<see cref="IInjector"/>) itself is being requested. This restriction ensures that the dependency graph will not change in runtime.</item>
        /// </list>
        /// </summary>
        /// <remarks>By default, scopes are permissives.</remarks>
        public bool StrictDI { get; set; }

        /// <summary>
        /// Specifies whether the created scopes should implement the <see cref="IServiceProvider"/> interface.
        /// </summary>
        /// <remarks>Setting this property to true configures the <see cref="IInjector.Get(Type, object?)"/> method to not throw if the requested dependency cannot be found.</remarks>
        public bool SupportsServiceProvider { get; set; }

        /// <summary>
        /// Specifies the service resolution mode.
        /// </summary>
        /// <remarks>It's recommended to use <i>Ahead Of Time</i> resolution because of its performance benefits, although <i>Just In Time</i> resolution is also useful when you want to mock the <see cref="IInjector"/> invocations.</remarks>
        public ServiceResolutionMode ServiceResolutionMode { get; set; } = ServiceResolutionMode.AOT;

        /// <summary>
        /// Specifies the maximum amount of time to wait to acquire the resolution lock.
        /// </summary>
        /// <remarks>This property is introduced mostly for debug purposes, it's recommended not to change it.</remarks>
        public TimeSpan ResolutionLockTimeout { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// The default options.
        /// </summary>
        /// <remarks>
        /// Modifying the <see cref="Default"/> value will impact the whole system. Therefore it's recommended to use the <i>with</i> pattern to set options when creating a scope:
        /// <code>
        /// ScopeFactory.Create(svcs => { ... }, ScopeOptions.Default with { ServiceResolutionMode = ... });
        /// </code>
        /// </remarks>
        public static ScopeOptions Default { get; } = new();
    }
}
