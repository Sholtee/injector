/********************************************************************************
* DuckFactory.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

using NUnit.Framework;

[assembly: InternalsVisibleTo("System.Object_Solti.Utils.Proxy.Tests.DuckFactoryTests.IMyEntity_Duck")]

namespace Solti.Utils.Proxy.Tests
{
    using DI.Internals;
    using Proxy.Generators;

    [TestFixture]
    public class DuckFactoryTests
    {
        internal interface IMyEntity { }

        [Test]
        public void Create_ShouldCacheTheGeneratedAssembly() 
        {
            string cacheDir = TypeGeneratorExtensions.GetCacheDirectory<IMyEntity, DuckGenerator<IMyEntity, object>>();
            Assert.That(!Directory.Exists(cacheDir));

            bool oldVal = DuckFactory.PreserveProxyAssemblies;
            DuckFactory.PreserveProxyAssemblies = true;

            try
            {
                Assert.DoesNotThrow(() => DuckFactory.Create(new object()).Like<IMyEntity>());
                Assert.That(Directory.EnumerateFiles(cacheDir).Any());
            }
            finally 
            {
                DuckFactory.PreserveProxyAssemblies = oldVal;

                if (Directory.Exists(cacheDir))
                    Directory.Delete(cacheDir, true);
            }
        }
    }
}
