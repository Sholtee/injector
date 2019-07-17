/********************************************************************************
* ProxyGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    public interface IFoo<T> // szerepeljen a "T"
    {
        int Foo<TT>(int a, out string b, ref TT c);
        void Bar();
        T Prop { get; set; }
    }

    [TestFixture]
    public sealed class ProxyGeneratorTests
    {
        [Test]
        public void DeclareLocal_ShouldDeclareTheDesiredLocalVariable()
        {
            Assert.That(ProxyGenerator.DeclareLocal<string[]>("paramz").NormalizeWhitespace().ToFullString(), Is.EqualTo("System.String[] paramz;"));
            Assert.That(ProxyGenerator.DeclareLocal<object[]>("args", ProxyGenerator.CreateArgumentsArray(GetMethod(nameof(Foo)))).NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Object[] args = new System.Object[]{a, default(System.String), c};"));
        }

        private static readonly MethodInfo
            Foo = GetMethod(nameof(Foo)),
            Bar = GetMethod(nameof(Bar)); 

        [Test]
        public void CreateArgumentsArray_ShouldCreateAnObjectArrayFromTheArguments()
        {
            Assert.That(ProxyGenerator.CreateArgumentsArray(Foo).NormalizeWhitespace().ToFullString(), Is.EqualTo("new System.Object[]{a, default(System.String), c}"));

            Assert.That(ProxyGenerator.CreateArgumentsArray(Bar).NormalizeWhitespace().ToFullString(), Is.EqualTo("new System.Object[]{}"));          
        }

        [Test]
        public void DeclareMethod_ShouldDeclareTheDesiredMethod()
        {
            Assert.That(ProxyGenerator.DeclareMethod(Foo).NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Int32 Solti.Utils.DI.Internals.Tests.IFoo<T>.Foo<TT>(System.Int32 a, out System.String b, ref TT c)"));
        }

        [Test]
        public void AssignByRefParameters_ShouldAssignByRefParameters()
        {
            IReadOnlyList<ExpressionStatementSyntax> assignments = ProxyGenerator.AssignByRefParameters(Foo, ProxyGenerator.DeclareLocal<object[]>("args"));

            Assert.That(assignments.Count, Is.EqualTo(2));
            Assert.That(assignments[0].NormalizeWhitespace().ToFullString(), Is.EqualTo("b = (System.String)args[1];"));
            Assert.That(assignments[1].NormalizeWhitespace().ToFullString(), Is.EqualTo("c = (TT)args[2];"));
        }

        [Test]
        public void SelfCallExpression_ShouldCreateAnExpressionCallingTheActualMethod()
        {
            Assert.That(ProxyGenerator.SelfCallExpression(Foo).NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Linq.Expressions.Expression<System.Action<Solti.Utils.DI.Internals.Tests.IFoo<T>>> callExpr = i => i.Foo(a, out b, ref c);"));
        }

        [Test]
        public void CreateType_ShouldCreateTheDesiredType()
        {
            Assert.That(ProxyGenerator.CreateType<int[]>().NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Int32[]"));
            Assert.That(ProxyGenerator.CreateType<IEnumerable<int[]>>().NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Collections.Generic.IEnumerable<System.Int32[]>"));
            Assert.That(ProxyGenerator.CreateType<int[,]>().NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Int32[,]"));
            Assert.That(ProxyGenerator.CreateType(typeof(IEnumerable<>)).NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Collections.Generic.IEnumerable<T>"));
            Assert.That(ProxyGenerator.CreateType<IEnumerable<IEnumerable<string>>>().NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<System.String>>"));
        }

        [Test]
        public void AcquireMethodInfo_ShouldGetAMethodInfoInstance()
        {
            Assert.That(ProxyGenerator.AcquireMethodInfo(ProxyGenerator.SelfCallExpression(Foo)).NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Reflection.MethodInfo currentMethod = ((System.Linq.Expressions.MethodCallExpression)callExpr.Body).Method;"));
        }

        private static MethodInfo GetMethod(string name) => typeof(IFoo<>).GetMethod(name);
    }
}
