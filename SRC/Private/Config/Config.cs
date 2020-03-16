/********************************************************************************
* Config.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes the configuration of this library. You can change settings programmatically or by editing the "Injector.config.json" file.
    /// </summary>
    public partial class Config
    {
        private static Config CreateInstance()
        {
            string configFile = Path.ChangeExtension(typeof(Config).Assembly().Location, "config.json");

            if (File.Exists(configFile))
                return JsonSerializer.Deserialize<Config>(File.ReadAllText(configFile));

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
        public Dictionary<string, object> CustomSettings { get; set; }
    }
}
