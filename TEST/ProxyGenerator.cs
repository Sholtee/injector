/********************************************************************************
* ProxyGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;

using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    [TestFixture]
    public sealed class ProxyGeneratorTests
    {
        [Test]
        public void DeclareLocal_ShouldDeclareALocalVariable()
        {
            Assert.That(ProxyGenerator.DeclareLocal<string[]>("paramz").ToFullString(), Is.EqualTo("System.String[] paramz;"));
        }

        public void Foo<T>(int a, out string b, T c)
        {
            b = "cica";
        }

        [Test]
        public void CreateArgumentsArray_ShouldCreateAnObjectArrayFromTheArguments()
        {
            Assert.That(ProxyGenerator.CreateArgumentsArray(GetMethod(nameof(Foo))).ToFullString(), Is.EqualTo("new object {a, default(System.String), c}"));

            Assert.That(ProxyGenerator.CreateArgumentsArray(GetMethod(nameof(CreateArgumentsArray_ShouldCreateAnObjectArrayFromTheArguments))).ToFullString(), Is.EqualTo("new object {}"));

            MethodInfo GetMethod(string name) => GetType().GetMethod(name);
        }

        [Test]
        public void DeclareMethod_ShouldDeclareAMethod()
        {
            Assert.That(ProxyGenerator.DeclareMethod(GetType().GetMethod(nameof(Foo))).ToFullString(), Is.EqualTo("public void Foo<T>(System.Int32 a, out System.String b, T c)"));
        }
    }
}
