/********************************************************************************
* TypeGeneratorExtensions.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;

namespace Solti.Utils.DI.Internals
{
    using Primitives;
    using Proxy.Abstractions;

    internal static class TypeGeneratorExtensions
    {
        private static readonly Regex FindGenericArgs = new Regex("`\\d+$", RegexOptions.Compiled);

        [SuppressMessage("Globalization", "CA1304:Specify CultureInfo")]
        public static string GetCacheDirectory<TInterface, TTypeGenerator>() where TTypeGenerator : TypeGenerator<TTypeGenerator>, new()
        {
            return Path.Combine
            (
                Path.GetTempPath(),
                $".{FindGenericArgs.Replace(typeof(TTypeGenerator).Name.ToLower(), m => string.Empty)}",
                GetVersion<TTypeGenerator>(),
                typeof(TInterface).GetFriendlyName(),
                GetVersion<TInterface>()
            );

            static string GetVersion<T>() => typeof(T).Assembly.GetName().Version.ToString();
        }
   
        public static void SetCacheDirectory<TInterface, TTypeGenerator>() where TTypeGenerator : TypeGenerator<TTypeGenerator>, new()
        {
            string cacheDir = GetCacheDirectory<TInterface, TTypeGenerator>();
            Debug.WriteLine($"Cache directory for {typeof(TInterface)} is set to '{cacheDir}'");

            Directory.CreateDirectory(cacheDir);
            TypeGenerator<TTypeGenerator>.CacheDirectory = cacheDir;           
        }
    }
}
