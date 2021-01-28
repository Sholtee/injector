/********************************************************************************
* Config.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes the configuration of this library. You can change settings programmatically or by editing the "runtimeconfig.template.json" file.
    /// </summary>
    /// <example>
    /// <code>
    /// {
    ///   "configProperties": {
    ///     "ServiceContainer.MaxChildCount": 512,
    ///     "Injector.StrictDI": false,
    ///     "Injector.MaxSpawnedTransientServices": 512
    ///   }
    /// }
    /// </code>
    /// </example>
    public partial class Config
    {
        private static T GetValue<T>(string name) => AppContext.GetData(name) is T result ? result : default!;

        /// <summary>
        /// The <see cref="Config"/> instance.
        /// </summary>
        public static Config Value { get; private set; } = new Config();
#if DEBUG
        internal static void Reset() =>
            //
            // Mivel a referenciak irasa atomi muvelet ezert nem kell lock a Reset() miatt
            // (ami amugy is csak tesztekben van hasznalva).
            //

            Value = new Config();
#endif
    }
}
