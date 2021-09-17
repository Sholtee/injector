﻿/********************************************************************************
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
    public class ScopeOptions
    {
        /// <summary>
        /// Instructs the <see cref="IInjector"/> to throw if a service being requested has a dependency that should live shorter than the service should (e.g.: a <see cref="Lifetime.Singleton"/> service cannot have <see cref="Lifetime.Transient"/> dependency).
        /// </summary>
        /// <remarks>This option is available only when <see cref="SafeMode"/> is true.</remarks>
        public bool StrictDI { get; init; }

        /// <summary>
        /// The maximum number of <see cref="Lifetime.Transient"/> service instances can be held by the <see cref="IInjector"/>.
        /// </summary>
        public int MaxSpawnedTransientServices { get; init; } = 512;

        /// <summary>
        /// Specifies whether the created scopes should implement the <see cref="IServiceProvider"/> interface.
        /// </summary>
        /// <remarks>Setting this property to true configures the <see cref="IInjector.Get(Type, string?)"/> method to not throw if the requested dependency cannot be found.</remarks>
        public bool SupportsServiceProvider { get; init; }

        /// <summary>
        /// Enables some extra validations.
        /// </summary>
        /// <remarks>Disable this feature when the performance is important.</remarks>
        public bool SafeMode { get; init; } = true;
    }
}
