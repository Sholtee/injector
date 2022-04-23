/********************************************************************************
* ExperimantelScope.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    [TestFixture]
    public class ExperimantelScopeTests
    {
        [Test]
        public void VariableCapturingTest()
        {
            Func<int>[] fns = new Func<int>[10];

            for (int i = 0; i < 10; i++)
            {
                int tmp = i;
                fns[i] = () => tmp;
            }

            for (int j = 0; j < 10; j++)
                Assert.That(fns[j](), Is.EqualTo(j));
        }

        [Test]
        public void VariableCapturingTest2()
        {
            object[] objs = new object[] { new(), new(), new() };

            Func<object>[] fns = new Func<object>[objs.Length];

            int i = 0;
            foreach (object obj in objs)
            {
                fns[i++] = () => obj;
            }

            for (i = 0; i < objs.Length; i++)
                Assert.That(fns[i](), Is.SameAs(objs[i]));
        }
    }
}
