﻿/********************************************************************************
* Injector.Config.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    public partial class Config 
    {
        /// <summary>
        /// Defines several options to control the behavior of the <see cref="IInjector"/> objects.
        /// </summary>
        public class InjectorConfig
        {
            /// <summary>
            /// Instructs the injector to throw if a service being requested has a dependency that should live shorter than the service should (e.g.: a <see cref="Lifetime.Singleton"/> service can not have <see cref="Lifetime.Transient"/> dependency).
            /// </summary>
            public bool StrictDI { get; set; } = GetValue<bool?>("Injector.StrictDI") ?? false;

            /// <summary>
            /// The maximum number of <see cref="Lifetime.Transient"/> service instances can be held by the <see cref="IInjector"/>.
            /// </summary>
            public int MaxSpawnedTransientServices { get; set; } = GetValue<int?>("Injector.MaxSpawnedTransientServices") ?? 512;
        }

        /// <summary>
        /// <see cref="InjectorConfig"/>.
        /// </summary>
        public InjectorConfig Injector { get; } = new InjectorConfig();
    }
}