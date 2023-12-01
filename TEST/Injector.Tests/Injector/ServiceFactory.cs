/********************************************************************************
* ServiceFactory.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Interfaces;
    using Internals;

    public partial class InjectorTests
    {
        [Test]
        public void GetOrCreateInstance_ShouldThrowOnDisposedScope()
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_1, Implementation_1>(Lifetime.Scoped));

            IServiceActivator fact = (IServiceActivator) Root.CreateScope();
            fact.Dispose();

            Assert.Throws<ObjectDisposedException>(() => fact.GetOrCreateInstance(new MissingServiceEntry(typeof(IInterface_1), null)));
        }

        [Test]
        public void GetOrCreateInstance_ShouldBeNullChecked()
        {
            Root = ScopeFactory.Create(svcs => svcs.Service<IInterface_1, Implementation_1>(Lifetime.Scoped));

            using IServiceActivator fact = (IServiceActivator) Root.CreateScope();;

            Assert.Throws<ArgumentNullException>(() => fact.GetOrCreateInstance(null));
        }
    }
}
