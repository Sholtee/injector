/********************************************************************************
* Lazy.cs                                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using Moq;
using NUnit.Framework;

namespace Solti.Utils.DI.Tests
{
    using Properties;

    [TestFixture]
    public sealed partial class InjectorTests
    {
        [TestCase(Lifetime.Transient)]
        [TestCase(Lifetime.Singleton)]
        public void Injector_Lazy_ShouldCallTheResolverOnlyOnce(Lifetime lifetime)
        {
            var mockResolver = new Mock<IResolver>(MockBehavior.Strict);
            mockResolver
                .Setup(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))))
                .Returns(typeof(Implementation_1));

            Injector.Lazy<IInterface_1>(mockResolver.Object, lifetime);

            //
            // Az elso Get()-eleskor kell hivja a rendszer a resolver-t
            //

            mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))), Times.Never);
            var instance = Injector.Get<IInterface_1>();
            mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))), Times.Once);

            Assert.That(instance, Is.InstanceOf<Implementation_1>());
            (lifetime == Lifetime.Singleton ? Assert.AreSame : (Action<object, object>) Assert.AreNotSame)(instance, Injector.Get<IInterface_1>());

            //
            // De csak is egyszer.
            //

            mockResolver.Verify(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))), Times.Once);
        }

        [Test]
        public void Injector_Lazy_ShouldBeTypeChecked()
        {
            var mockResolver = new Mock<IResolver>(MockBehavior.Strict);
            mockResolver
                .Setup(r => r.Resolve(It.Is<Type>(t => t == typeof(IInterface_1))))
                .Returns(typeof(Implementation_2));

            Injector.Lazy<IInterface_1>(mockResolver.Object);

            Assert.Throws<InvalidOperationException>(() => Injector.Get<IInterface_1>(), string.Format(Resources.NOT_ASSIGNABLE, typeof(IInterface_1), typeof(object)));
        }
    }
}
