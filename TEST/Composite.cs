﻿/********************************************************************************
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
            public MyComposite(IMyComposite parent) : base(parent)
            {
            }

            public MyComposite() : this(null)
            {
            }
        }

        [Test]
        public void Composite_Dispose_ShouldFreeTheChildrenRecursively() 
        {
            IMyComposite 
                root = new MyComposite(),
                child = new MyComposite(root),
                grandChild = new MyComposite(child);

            root.Dispose();

            Assert.Throws<ObjectDisposedException>(grandChild.Dispose);
            Assert.Throws<ObjectDisposedException>(child.Dispose);
        }

        [Test]
        public void Composite_Dispose_ShouldRemoveTheChildFromTheParentsChildrenList() 
        {
            IMyComposite
                root = new MyComposite(),
                child = new MyComposite(root);

            new MyComposite(root); // harmadik

            Assert.That(root.Children.Count, Is.EqualTo(2));
            child.Dispose();
            Assert.That(root.Children.Count, Is.EqualTo(1));
        }

        [Test]
        public void Composite_AddChild_ShouldValidate() 
        {
            IMyComposite 
                root = new MyComposite(),
                child = new MyComposite(root);

            Assert.Throws<ArgumentNullException>(() => root.Children.Add(null));
            Assert.Throws<Exception>(() => root.Children.Add(child), Resources.NOT_NULL);
        }

        [Test]
        public void Composite_RemoveChild_ShouldValidate()
        {
            IMyComposite
                root = new MyComposite(),
                child = new MyComposite(root);

            Assert.Throws<ArgumentNullException>(() => root.Children.Remove(null));
            Assert.That(() => root.Children.Remove(new MyComposite()), Is.False);

            //
            // Nem lett felszabaditva.
            //

            Assert.DoesNotThrow(child.Dispose);
            Assert.That(root.Children.Count, Is.EqualTo(0));
        }
    }
}
