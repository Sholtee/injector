/********************************************************************************
* Composite.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using Properties;

    [TestFixture]
    public sealed class CompositeTests
    {
        private interface IMyComposite : IComposite<IMyComposite> 
        { 
        }

        private class MyComposite : Composite<IMyComposite>, IMyComposite
        {
            protected MyComposite(IMyComposite parent): base(parent)
            { 
            }

            public MyComposite() : this(null) 
            { 
            }

            public override IMyComposite CreateChild() => new MyComposite(this);
        }

        [Test]
        public void Composite_Should_DisposeTheChildrenRecursively() 
        {
            IMyComposite 
                root = new MyComposite(),
                child = root.CreateChild(),
                grandChild = child.CreateChild();

            root.Dispose();

            Assert.Throws<ObjectDisposedException>(grandChild.Dispose);
            Assert.Throws<ObjectDisposedException>(child.Dispose);
        }

        [Test]
        public void Composite_Dispose_Should_RemoveTheChildFromTheParentsChildrenList() 
        {
            IMyComposite
                root = new MyComposite(),
                child = root.CreateChild();

            root.CreateChild(); // harmadik

            Assert.That(root.Children.Count, Is.EqualTo(2));
            child.Dispose();
            Assert.That(root.Children.Count, Is.EqualTo(1));
        }

        [Test]
        public void Composite_ChildrenList_ShouldBeACopy() 
        {
            IMyComposite root = new MyComposite();

            root.CreateChild();

            Assert.AreNotSame(root.Children, root.Children);
        }

        [Test]
        public void Composite_AddChild_Should_Validate() 
        {
            IMyComposite 
                root = new MyComposite(),
                child = root.CreateChild();

            Assert.Throws<ArgumentNullException>(() => root.AddChild(null));
            Assert.Throws<InvalidOperationException>(() => root.AddChild(child), Resources.NOT_ORPHAN);
        }

        [Test]
        public void Composite_RemoveChild_Should_Validate()
        {
            IMyComposite
                root = new MyComposite(),
                child = root.CreateChild();

            Assert.Throws<ArgumentNullException>(() => root.RemoveChild(null));
            Assert.Throws<InvalidOperationException>(() => root.RemoveChild(new MyComposite()), Resources.INVALID_PARENT);
            Assert.DoesNotThrow(() => root.RemoveChild(child));
            Assert.IsNull(child.Parent);

            //
            // Nem lett felszabaditva.
            //

            Assert.DoesNotThrow(child.Dispose);
        }
    }
}
