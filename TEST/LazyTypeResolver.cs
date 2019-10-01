/********************************************************************************
* LazyTypeResolver.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;
using System.Runtime.Loader;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.LazyTypeResolver.Tests
{
    using Properties;

    [TestFixture]
    public class LazyTypeResolverTests
    {
        private static readonly string
            AsmPath  = Path.Combine(Path.GetDirectoryName(typeof(LazyTypeResolverTests).Assembly.Location), "Injector.dll"),
            TypeName = "Solti.Utils.DI.Internals.Disposable";

        [Test]
        public void Constructor_ShouldThrowOnNonInterfaceTarget()
        {
            Assert.Throws<ArgumentException>(() => new LazyTypeResolver<object>(AsmPath, TypeName), Resources.NOT_AN_INTERFACE);
        }

        [Test]
        public void Resolve_ShouldLoadTheContainingAssemblyOnlyOnce()
        {     
            var mockLoadCtx = new Mock<IAssemblyLoadContext>(MockBehavior.Strict);
            mockLoadCtx
                .Setup(l => l.LoadFromAssemblyPath(It.IsAny<string>()))
                .Returns<string>(AssemblyLoadContext.Default.LoadFromAssemblyPath);
            
            var resolver = new LazyTypeResolver<IDisposable>(AsmPath, TypeName, mockLoadCtx.Object);

            mockLoadCtx.Verify(l => l.LoadFromAssemblyPath(It.IsAny<string>()), Times.Never);

            Assert.DoesNotThrow(() => resolver.Resolve(typeof(IDisposable)));
            resolver.Resolve(typeof(IDisposable));            

            mockLoadCtx.Verify(l => l.LoadFromAssemblyPath(It.Is<string>(p => p == AsmPath)), Times.Once);
        }

        [Test]
        public void Resolve_ShouldReturnTheSameTypeForTheSameInterface()
        {
            var resolver = new LazyTypeResolver<IDisposable>(AsmPath, TypeName);
            Assert.AreSame(resolver.Resolve(typeof(IDisposable)), resolver.Resolve(typeof(IDisposable)));
        }

        [Test]
        public void Resolve_ShouldThrowOnUnsupportedType()
        {
            Assert.Throws<NotSupportedException>(() => new LazyTypeResolver<IDisposable>(AsmPath, TypeName).Resolve(typeof(IInjector)));
        }
    }
}
