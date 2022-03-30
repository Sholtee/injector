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
                int k = i;
                fns[i] = () => k;
            }

            for (int j = 0; j < 10; j++)
                Assert.That(fns[j](), Is.EqualTo(j));
        }
    }
}
