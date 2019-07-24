/********************************************************************************
* ProxyGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

    public class Foo : InterfaceInterceptor<IFoo<int>>
    {
        public override object Invoke(MethodInfo method, object[] args)
        {
            return 1;
        }

        public Foo(IFoo<int> target) : base(target)
        {
        }
    }

    [TestFixture]
    public sealed class ProxyGeneratorTests
    {
        [Test]
        public void DeclareLocal_ShouldDeclareTheDesiredLocalVariable()
        {
            Assert.That(ProxyGenerator.DeclareLocal<string[]>("paramz").NormalizeWhitespace().ToFullString(), Is.EqualTo("System.String[] paramz;"));
         }

        private static readonly MethodInfo
            Foo = GetMethod(nameof(Foo)),
            Bar = GetMethod(nameof(Bar));

        private static readonly PropertyInfo Prop = typeof(IFoo<int>).GetProperty(nameof(IFoo<int>.Prop));

        [Test]
        public void CreateArgumentsArray_ShouldCreateAnObjectArrayFromTheArguments()
        {
            Assert.That(ProxyGenerator.CreateArgumentsArray(Foo).NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Object[] args = new System.Object[]{a, default(System.String), c};"));

            Assert.That(ProxyGenerator.CreateArgumentsArray(Bar).NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Object[] args = new System.Object[0];"));          
        }

        [Test]
        public void DeclareMethod_ShouldDeclareTheDesiredMethod()
        {
            Assert.That(ProxyGenerator.DeclareMethod(Foo).NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Int32 Solti.Utils.DI.Internals.Tests.IFoo<T>.Foo<TT>(System.Int32 a, out System.String b, ref TT c)"));

            Type typeOfT = typeof(List<>).GetGenericArguments().Single();

            Assert.That(ProxyGenerator.DeclareMethod(typeOfT, "Foo", new[] {SyntaxKind.PrivateKeyword, SyntaxKind.StaticKeyword}, new[] {typeOfT.Name}, new Dictionary<string, Type>
            {
                {"param", typeOfT}
            }).NormalizeWhitespace().ToFullString(), Is.EqualTo("private static T Foo<T>(T param)"));
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
        public void CreateType_ShouldCreateTheDesiredType()
        {
            Assert.That(ProxyGenerator.CreateType<int[]>().NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Int32[]"));
            Assert.That(ProxyGenerator.CreateType<IEnumerable<int[]>>().NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Collections.Generic.IEnumerable<System.Int32[]>"));
            Assert.That(ProxyGenerator.CreateType<int[,]>().NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Int32[, ]"));
            Assert.That(ProxyGenerator.CreateType(typeof(IEnumerable<>)).NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Collections.Generic.IEnumerable<T>"));
            Assert.That(ProxyGenerator.CreateType(typeof(IEnumerable<int>[])).NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Collections.Generic.IEnumerable<System.Int32>[]"));
            Assert.That(ProxyGenerator.CreateType<IEnumerable<IEnumerable<string>>>().NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<System.String>>"));
        }

        [Test]
        public void AcquireMethodInfo_ShouldGetAMethodInfoInstance()
        {
            LocalDeclarationStatementSyntax currentMethod;

            IReadOnlyList<StatementSyntax> statements = ProxyGenerator.AcquireMethodInfo(Foo, out currentMethod);
            Assert.That(statements.Count, Is.EqualTo(3));
            Assert.That(statements.Last().NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Reflection.MethodInfo currentMethod = MethodAccess(i => i.Foo(a, out dummy_b, ref dummy_c));"));

            statements = ProxyGenerator.AcquireMethodInfo(Bar, out currentMethod);
            Assert.That(statements.Count, Is.EqualTo(1));
            Assert.That(statements.Last().NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Reflection.MethodInfo currentMethod = MethodAccess(i => i.Bar());"));
        }

        [Test]
        public void AcquirePropertyInfo_ShouldGetAPropertyInfoInstance()
        {
            Assert.That(ProxyGenerator.AcquirePropertyInfo(Prop).NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Reflection.PropertyInfo currentProperty = PropertyAccess(i => i.Prop);"));
        }

        [Test]
        public void CallInvoke_ShouldCallTheInvokeMetehodOnThis()
        {
            Assert.That(ProxyGenerator.CallInvoke
            (
                ProxyGenerator.DeclareLocal<MethodInfo>("currentMethod"),
                ProxyGenerator.DeclareLocal<object[]>("args")
            ).NormalizeWhitespace().ToFullString(), Is.EqualTo("Invoke(currentMethod, args)"));
        }

        [Test]
        public void ReturnResult_ShouldCreateTheProperExpression()
        {
            Assert.That(ProxyGenerator.ReturnResult(typeof(void), ProxyGenerator.DeclareLocal<object>("@void")).NormalizeWhitespace().ToFullString(), Is.EqualTo("return;"));
            Assert.That(ProxyGenerator.ReturnResult(typeof(List<int>), ProxyGenerator.DeclareLocal<object>("result")).NormalizeWhitespace().ToFullString(), Is.EqualTo("return (System.Collections.Generic.List<System.Int32>)result;"));
        }

        [Test]
        public void PropertyAccess_ShouldCreateTheProperMethod()
        {
            Assert.That(ProxyGenerator.PropertyAccess(typeof(IFoo<int>)).NormalizeWhitespace(eol: Environment.NewLine).ToFullString(), Is.EqualTo($"private static System.Reflection.PropertyInfo PropertyAccess<TResult>(System.Linq.Expressions.Expression<System.Func<Solti.Utils.DI.Internals.Tests.IFoo<System.Int32>, TResult>> propertyAccess){Environment.NewLine}{{{Environment.NewLine}    return (System.Reflection.PropertyInfo)((System.Linq.Expressions.MemberExpression)propertyAccess.Body).Member;{Environment.NewLine}}}"));
        }

        [Test]
        public void MethodAccess_ShouldCreateTheProperMethod()
        {
            Assert.That(ProxyGenerator.MethodAccess(typeof(IFoo<int>)).NormalizeWhitespace(eol: Environment.NewLine).ToFullString(), Is.EqualTo($"private static System.Reflection.MethodInfo MethodAccess(System.Linq.Expressions.Expression<System.Action<Solti.Utils.DI.Internals.Tests.IFoo<System.Int32>>> methodAccess){Environment.NewLine}{{{Environment.NewLine}    return ((System.Linq.Expressions.MethodCallExpression)methodAccess.Body).Method;{Environment.NewLine}}}"));
        }

        [Test]
        public void DeclareProperty_ShouldDeclareTheDesiredProperty()
        {
            // Nem gond h "get set" nincs, ez azert van mert nincs torzs meghatarozva.
            Assert.That(ProxyGenerator.DeclareProperty(Prop, null, null).NormalizeWhitespace().ToFullString(), Is.EqualTo($"System.Int32 Solti.Utils.DI.Internals.Tests.IFoo<System.Int32>.Prop"));
        }

        [Test]
        public void GenerateProxyMethod_Test()
        {
            Assert.That(ProxyGenerator.GenerateProxyMethod(Foo).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("FooSrc.txt")));
            Assert.That(ProxyGenerator.GenerateProxyMethod(Bar).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("BarSrc.txt")));
        }

        [Test]
        public void GenerateProxyProperty_Test()
        {
            Assert.That(ProxyGenerator.GenerateProxyProperty(Prop).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("PropSrc.txt")));
        }

        [Test]
        public void GenerateProxyClass_Test()
        {
            Assert.That(ProxyGenerator.GenerateProxyClass(typeof(Foo), typeof(IFoo<int>)).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText("ClsSrc.txt")));
        }

        [Test]
        public void GeneratedProxy_Test()
        {
            IFoo<int> proxy = GeneratedProxy<IFoo<int>, Foo>.Instantiate(new []{typeof(IFoo<int>)}, (object) null);

            string a, b = string.Empty;

            Assert.That(proxy.Prop, Is.EqualTo(1));
            Assert.That(proxy.Foo(0, out a, ref b), Is.EqualTo(1));
        }

        [Test]
        public void CallTarget_ShouldInvokeTheTargetMethod()
        {
            Assert.That(ProxyGenerator.CallTarget(Foo).NormalizeWhitespace().ToFullString(), Is.EqualTo("return Target.Foo(a, out b, ref c);"));
            Assert.That(ProxyGenerator.CallTarget(Bar).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo("{\n    Target.Bar();\n    return;\n}"));
        }

        [Test]
        public void ReadTarget_ShouldReadTheGivenProperty()
        {
            Assert.That(ProxyGenerator.ReadTarget(Prop).NormalizeWhitespace().ToFullString(), Is.EqualTo("return Target.Prop;"));
        }

        [Test]
        public void WriteTarget_ShouldWriteTheGivenProperty()
        {
            Assert.That(ProxyGenerator.WriteTarget(Prop).NormalizeWhitespace().ToFullString(), Is.EqualTo("Target.Prop = value;"));
        }

        private static MethodInfo GetMethod(string name) => typeof(IFoo<>).GetMethod(name);
    }
}
