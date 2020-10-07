/********************************************************************************
* Config.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes the configuration of this library. You can change settings programmatically or by editing the "runtimeconfig.template.json" file.
    /// </summary>
    /// <example>
    /// <code>
    /// {
    ///   "configProperties": {
    ///     "DI": {
    ///       "Composite": {
    ///         "MaxChildCount": 512
    ///       },
    ///       "Injector": {
    ///         "StrictDI": false,
    ///         "MaxSpawnedTransientServices": 512
    ///       }
    ///     }
    ///   }
    /// }
    /// </code>
    /// </example>
    public partial class Config
    {
        private static Config CreateInstance()
        {
            object? data = AppContext.GetData("DI");

            if (data != null)
                return JsonSerializer.Deserialize<Config>((string) data);

            return new Config();
        }

        /// <summary>
        /// The <see cref="Config"/> instance.
        /// </summary>
        public static Config Value { get; private set; } = CreateInstance();

        internal static void Reset() =>
            //
            // Mivel a referenciak irasa atomi muvelet ezert nem kell lock a Reset() miatt
            // (ami amugy is csak tesztekben van hasznalva).
            //

            Value = CreateInstance();

        /// <summary>
        /// Custom settings.
        /// </summary>
        [JsonExtensionData]
        [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Writable property is required by JsonSerializer.")]
        public Dictionary<string, object>? CustomSettings { get; set; }
    }
}
