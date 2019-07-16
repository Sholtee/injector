/********************************************************************************
* ProxyGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            Assert.That(ProxyGenerator.DeclareLocal<object[]>("args", ProxyGenerator.CreateArgumentsArray(GetMethod(nameof(Foo)))).ToFullString(), Is.EqualTo("System.Object[] args = new System.Object[]{a, default(System.String), c};"));
        }

        public void Foo<T>(int a, out string b, T c)
        {
            b = "cica";
        }

        [Test]
        public void CreateArgumentsArray_ShouldCreateAnObjectArrayFromTheArguments()
        {
            Assert.That(ProxyGenerator.CreateArgumentsArray(GetMethod(nameof(Foo))).ToFullString(), Is.EqualTo("new System.Object[]{a, default(System.String), c}"));

            Assert.That(ProxyGenerator.CreateArgumentsArray(GetMethod(nameof(CreateArgumentsArray_ShouldCreateAnObjectArrayFromTheArguments))).ToFullString(), Is.EqualTo("new System.Object[]{}"));          
        }

        [Test]
        public void DeclareMethod_ShouldDeclareAMethod()
        {
            Assert.That(ProxyGenerator.DeclareMethod(GetType().GetMethod(nameof(Foo))).ToFullString(), Is.EqualTo("public void Foo<T>(System.Int32 a, out System.String b, T c)"));
        }

        [Test]
        public void AssignByRefParameters_ShouldAssignByRefParameters()
        {
            IReadOnlyList<ExpressionStatementSyntax> assignments = ProxyGenerator.AssignByRefParameters(GetMethod(nameof(Foo)), ProxyGenerator.DeclareLocal<object[]>("args"));

            Assert.That(assignments.Count, Is.EqualTo(1));
            Assert.That(assignments.Single().ToFullString(), Is.EqualTo("b = (System.String)args[1];"));
        }

        private MethodInfo GetMethod(string name) => GetType().GetMethod(name);
    }
}
