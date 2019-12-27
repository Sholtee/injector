/********************************************************************************
* Config.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
#if !NETSTANDARD1_6
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.IO;

using System.Text.Json;
using System.Text.Json.Serialization;
#endif
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes the configuration of this libary. You can change settings programmatically or by editing the "Injector.config.json" file.
    /// </summary>
    public partial class Config
    {
        private static readonly ReaderWriterLockSlim FLock = new ReaderWriterLockSlim();

        private static Config FValue = CreateInstance();

        private static Config CreateInstance() 
        {
#if !NETSTANDARD1_6
            string configFile = Path.ChangeExtension(typeof(Config).Assembly().Location, "config.json");

            if (File.Exists(configFile))
                return JsonSerializer.Deserialize<Config>(File.ReadAllText(configFile));
#endif
            return new Config();         
        }

        /// <summary>
        /// The <see cref="Config"/> instance.
        /// </summary>
        public static Config Value 
        {
            get 
            {
                using (FLock.AcquireReaderLock())
                {
                    return FValue;
                }
            }
        }

        internal static void Reset() // tesztekhez
        {
            using (FLock.AcquireWriterLock())
            {
                FValue = CreateInstance();
            }
        }

#if !NETSTANDARD1_6
        /// <summary>
        /// Custom settings.
        /// </summary>
        [JsonExtensionData]
        [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Writable property is required by JsonSerializer.")]
        public Dictionary<string, object> CustomSettings { get; set; }
#endif
    }
}
