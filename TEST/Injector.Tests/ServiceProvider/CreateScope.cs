/********************************************************************************
* CreateScope.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Interfaces;

    public partial class InjectorTests
    {
        [Test]
        public void ServiceProvider_CreateScope_ShouldThrowIfTheFeatureIsNotSupported()
        {
            Root = ScopeFactory.Create
            (
                svcs => { },
                new ScopeOptions { SupportsServiceProvider = false }
            );

            Assert.Throws<NotSupportedException>(() => Root.CreateScope(out _));
        }

        [Test]
        public void ServiceProvider_CreateScope_ShouldBeNullChecked()
        {
            Assert.Throws<ArgumentNullException>(() => IScopeFactoryExtensions.CreateScope(null, out _));
        }
    }
}
