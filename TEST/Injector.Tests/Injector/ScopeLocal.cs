/********************************************************************************
* ScopeLocal.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Interfaces;

    public partial class InjectorTests
    {
        [Test]
        public void Injector_AssignScopeLocal_ShouldAssignTheNewValue([Values("cica", null)] object key)
        {
            Root = ScopeFactory.Create(svcs => svcs.SetupScopeLocal<IList>(key));

            using IInjector injector = Root.CreateScope();

            Mock<IList> mockService = new();

            injector.AssignScopeLocal(key, mockService.Object);

            Assert.That(injector.Get<IList>(key), Is.EqualTo(mockService.Object));
        }

        [Test]
        public void Injector_AssignedScopeLocal_ShouldNeverGetDisposed()
        {
            Root = ScopeFactory.Create(svcs => svcs.SetupScopeLocal<IDisposable>());

            Mock<IDisposable> mockService = new(MockBehavior.Loose);

            using IInjector injector = Root.CreateScope();
            {
                injector.AssignScopeLocal(mockService.Object);
                injector.Get<IDisposable>();
            }

            mockService.Verify(x => x.Dispose(), Times.Never);
        }

        [Test]
        public void Injector_AssignScopeLocal_ShouldThrowOnMultipleAssign()
        {
            Root = ScopeFactory.Create(svcs => svcs.SetupScopeLocal<IList>());

            using IInjector injector = Root.CreateScope();

            injector.AssignScopeLocal(new Mock<IList>().Object);

            Assert.Throws<InvalidOperationException>(() => injector.AssignScopeLocal(new Mock<IList>().Object));
        }

        [Test]
        public void Injector_AssignedScopeLocal_MustHaveAValue()
        {
            Root = ScopeFactory.Create(svcs => svcs.SetupScopeLocal<IMyService>());

            using IInjector injector = Root.CreateScope();

            Assert.Throws<InvalidOperationException>(() => injector.Get<IMyService>());
        }

        [Test]
        public void Injector_AssignedScopeLocal_ShouldBeNullChecked()
        {
            Root = ScopeFactory.Create(svcs => svcs.SetupScopeLocal<IMyService>());

            using IInjector injector = Root.CreateScope();

            Assert.Throws<ArgumentNullException>(() => injector.AssignScopeLocal(null, null, new object()));
            Assert.Throws<ArgumentNullException>(() => injector.AssignScopeLocal(typeof(IMyService), null));
            Assert.Throws<ArgumentNullException>(() => injector.AssignScopeLocal<IMyService>(null));
            Assert.Throws<ArgumentNullException>(() => IInjectorAdvancedExtensions.AssignScopeLocal(null, typeof(IMyService), new object()));
        }
    }
}
