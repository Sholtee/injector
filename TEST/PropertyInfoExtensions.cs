/********************************************************************************
* PropertyInfoExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Reflection;

using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    [TestFixture]
    public sealed class PropertyInfoExtensions
    {
        [Test]
        public void FastSetVAlue_ShouldSetTheProperty()
        {
            PropertyInfo prop = (PropertyInfo) ((MemberExpression) ((Expression<Func<MyClass, int>>) (x => x.Prop)).Body).Member;

            var inst = new MyClass();
            prop.FastSetValue(inst, 10);

            Assert.That(inst.Prop, Is.EqualTo(10));
        }

        private sealed class MyClass
        {
            public int Prop { get; set; }
        }
    }
}
