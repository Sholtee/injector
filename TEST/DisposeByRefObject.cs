/********************************************************************************
* DisposeByRefObject.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using Internals;

    [TestFixture]
    public sealed class DisposeByRefObjectTests
    {
        [Test]
        public void AddRef_ShouldIncrementTheReferenceCount() 
        {
            var obj = new DisposeByRefObject();
            Assert.That(obj.RefCount, Is.EqualTo(1));
            Assert.That(obj.AddRef(), Is.EqualTo(2));
            Assert.That(obj.RefCount, Is.EqualTo(2));
        }

        [Test]
        public void Release_ShouldDecrementTheReferenceCount()
        {
            var obj = new DisposeByRefObject();
            Assert.That(obj.RefCount, Is.EqualTo(1));
            Assert.That(obj.Release(), Is.EqualTo(0));
            Assert.That(obj.RefCount, Is.EqualTo(0));
        }

        [Test]
        public void Release_ShouldDisposeTheObjectIfRefCountReachesTheZero() 
        {
            var obj = new DisposeByRefObject();
            obj.AddRef();
            Assert.That(obj.Release(), Is.EqualTo(1));
            Assert.That(obj.Disposed, Is.False);
            Assert.That(obj.Release(), Is.EqualTo(0));
            Assert.That(obj.Disposed);
        }
    }
}
