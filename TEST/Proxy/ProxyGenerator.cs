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
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using NUnit.Framework;

[assembly: InternalsVisibleTo("Solti.Utils.DI.Proxy.Tests.ProxyGeneratorTests.FooInterceptor_Solti.Utils.DI.Proxy.Tests.ProxyGeneratorTests.IFoo<System.Int32>_Proxy")]

namespace Solti.Utils.DI.Proxy.Tests
{
    using Proxy;
    using Internals;
    
    using static Internals.ProxyGeneratorBase;
    using static Internals.ProxyGenerator<ProxyGeneratorTests.IFoo<int>, ProxyGeneratorTests.FooInterceptor>;

    [TestFixture]
    public sealed class ProxyGeneratorTests
    {
        internal delegate void TestDelegate<in T>(object sender, T eventArg);

        internal interface IFoo<T> // direkt internal
        {
            int Foo<TT>(int a, out string b, ref TT c);
            void Bar();
            T Prop { get; set; }
            event TestDelegate<T> Event;
        }

        internal class FooInterceptor : InterfaceInterceptor<IFoo<int>> // direkt internal
        {
            public override object Invoke(MethodInfo method, object[] args, MemberInfo extra)
            {
                return 1;
            }

            public FooInterceptor(IFoo<int> target) : base(target)
            {
            }
        }

        public class ListInterceptor : InterfaceInterceptor<IList<int>>
        {
            public ListInterceptor(IList<int> target) : base(target)
            {
            }
        }

        [Test]
        public void DeclareLocal_ShouldDeclareTheDesiredLocalVariable()
        {
            Assert.That(DeclareLocal<string[]>("paramz").NormalizeWhitespace().ToFullString(), Is.EqualTo("System.String[] paramz;"));
         }

        private static readonly MethodInfo
            Foo = GetMethod(nameof(IFoo<int>.Foo)),
            Bar = GetMethod(nameof(IFoo<int>.Bar));

        private static readonly EventInfo
            Event = typeof(IFoo<int>).GetEvent(nameof(IFoo<int>.Event), BindingFlags.Public | BindingFlags.Instance);

        private static readonly PropertyInfo 
            Prop    = typeof(IFoo<int>).GetProperty(nameof(IFoo<int>.Prop)),
            Indexer = typeof(IList<int>).GetProperty("Item");

        [Test]
        public void CreateArgumentsArray_ShouldCreateAnObjectArrayFromTheArguments()
        {
            Assert.That(CreateArgumentsArray(Foo).NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Object[] args = new System.Object[]{a, default(System.String), c};"));

            Assert.That(CreateArgumentsArray(Bar).NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Object[] args = new System.Object[0];"));          
        }

        [Test]
        public void AssignByRefParameters_ShouldAssignByRefParameters()
        {
            IReadOnlyList<ExpressionStatementSyntax> assignments = AssignByRefParameters(Foo, DeclareLocal<object[]>("args"));

            Assert.That(assignments.Count, Is.EqualTo(2));
            Assert.That(assignments[0].NormalizeWhitespace().ToFullString(), Is.EqualTo("b = (System.String)args[1];"));
            Assert.That(assignments[1].NormalizeWhitespace().ToFullString(), Is.EqualTo("c = (TT)args[2];"));
        }

        [Test]
        public void CreateType_ShouldCreateTheDesiredType()
        {
            Assert.That(CreateType<int[]>().NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Int32[]"));
            Assert.That(CreateType<IEnumerable<int[]>>().NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Collections.Generic.IEnumerable<System.Int32[]>"));
            Assert.That(CreateType<int[,]>().NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Int32[, ]"));
            Assert.That(CreateType(typeof(IEnumerable<>)).NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Collections.Generic.IEnumerable<T>"));
            Assert.That(CreateType(typeof(IEnumerable<int>[])).NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Collections.Generic.IEnumerable<System.Int32>[]"));
            Assert.That(CreateType<IEnumerable<IEnumerable<string>>>().NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Collections.Generic.IEnumerable<System.Collections.Generic.IEnumerable<System.String>>"));
        }

        private class CicaNested<T>
        {
            public class Mica<TT>
            {
                public enum Hajj
                {                   
                }
            }
            public class Mica 
            {
                public class Hajj<TT, TTT> 
                { 
                }
            }
        }

        [Test]
        public void CreateType_ShouldHandleNestedTypes()
        {
            Assert.That(CreateType(typeof(Cica<>.Mica<>.Hajj)).NormalizeWhitespace().ToFullString(), Is.EqualTo("Solti.Utils.DI.Proxy.Tests.Cica<T>.Mica<TT>.Hajj"));
            Assert.That(CreateType(typeof(Cica<>.Mica.Hajj<,>)).NormalizeWhitespace().ToFullString(), Is.EqualTo("Solti.Utils.DI.Proxy.Tests.Cica<T>.Mica.Hajj<TT, TTT>"));
            Assert.That(CreateType<Cica<List<int>>.Mica<string>.Hajj>().NormalizeWhitespace().ToFullString(), Is.EqualTo("Solti.Utils.DI.Proxy.Tests.Cica<System.Collections.Generic.List<System.Int32>>.Mica<System.String>.Hajj"));
            Assert.That(CreateType(typeof(Cica<int>.Mica.Hajj<string, object>)).NormalizeWhitespace().ToFullString(), Is.EqualTo("Solti.Utils.DI.Proxy.Tests.Cica<System.Int32>.Mica.Hajj<System.String, System.Object>"));

            Assert.That(CreateType(typeof(CicaNested<>.Mica<>.Hajj)).NormalizeWhitespace().ToFullString(), Is.EqualTo("Solti.Utils.DI.Proxy.Tests.ProxyGeneratorTests.CicaNested<T>.Mica<TT>.Hajj"));
            Assert.That(CreateType(typeof(CicaNested<>.Mica.Hajj<,>)).NormalizeWhitespace().ToFullString(), Is.EqualTo("Solti.Utils.DI.Proxy.Tests.ProxyGeneratorTests.CicaNested<T>.Mica.Hajj<TT, TTT>"));
            Assert.That(CreateType<CicaNested<List<int>>.Mica<string>.Hajj>().NormalizeWhitespace().ToFullString(), Is.EqualTo("Solti.Utils.DI.Proxy.Tests.ProxyGeneratorTests.CicaNested<System.Collections.Generic.List<System.Int32>>.Mica<System.String>.Hajj"));
            Assert.That(CreateType(typeof(CicaNested<int>.Mica.Hajj<string, object>)).NormalizeWhitespace().ToFullString(), Is.EqualTo("Solti.Utils.DI.Proxy.Tests.ProxyGeneratorTests.CicaNested<System.Int32>.Mica.Hajj<System.String, System.Object>"));
        }

        [Test]
        public void AcquireMethodInfo_ShouldGetAMethodInfoInstance()
        {
            LocalDeclarationStatementSyntax currentMethod;

            IReadOnlyList<StatementSyntax> statements = AcquireMethodInfo(Foo, out currentMethod);
            Assert.That(statements.Count, Is.EqualTo(3));
            Assert.That(statements.Last().NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Reflection.MethodInfo currentMethod = MethodAccess(() => Target.Foo(a, out dummy_b, ref dummy_c));"));

            statements = AcquireMethodInfo(Bar, out currentMethod);
            Assert.That(statements.Count, Is.EqualTo(1));
            Assert.That(statements.Last().NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Reflection.MethodInfo currentMethod = MethodAccess(() => Target.Bar());"));
        }

        [Test]
        public void CallInvoke_ShouldCallTheInvokeMetehodOnThis()
        {
            LocalDeclarationStatementSyntax
                currentMethod = DeclareLocal<MethodInfo>(nameof(currentMethod)),
                args = DeclareLocal<object[]>(nameof(args));

            Assert.That(CallInvoke
            (
                currentMethod,
                args
            ).NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Object result = Invoke(currentMethod, args);"));
        }

        [Test]
        public void ReturnResult_ShouldCreateTheProperExpression()
        {
            Assert.That(ReturnResult(typeof(void), DeclareLocal<object>("@void")).NormalizeWhitespace().ToFullString(), Is.EqualTo("return;"));
            Assert.That(ReturnResult(typeof(List<int>), DeclareLocal<object>("result")).NormalizeWhitespace().ToFullString(), Is.EqualTo("return (System.Collections.Generic.List<System.Int32>)result;"));
        }

        [Test]
        public void DeclareProperty_ShouldDeclareTheDesiredProperty()
        {
            Assert.That(DeclareProperty(Prop, SyntaxFactory.Block(), SyntaxFactory.Block()).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo("System.Int32 Solti.Utils.DI.Proxy.Tests.ProxyGeneratorTests.IFoo<System.Int32>.Prop\n{\n    get\n    {\n    }\n\n    set\n    {\n    }\n}"));
        }

        [Test]
        public void GenerateProxyMethod_Test()
        {
            Assert.That(GenerateProxyMethod(Foo).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText(Path.Combine("Proxy", "FooSrc.txt"))));
            Assert.That(GenerateProxyMethod(Bar).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText(Path.Combine("Proxy", "BarSrc.txt"))));
        }

        [Test]
        public void GenerateProxyProperty_Test()
        {
            Assert.That(GenerateProxyProperty(Prop).Last().NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText(Path.Combine("Proxy", "PropSrc.txt"))));
        }

        [Test]
        public void GenerateProxyIndexer_Test()
        {
            Assert.That(GenerateProxyIndexer(Indexer, SyntaxFactory.IdentifierName($"F{Indexer.Name}")).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText(Path.Combine("Proxy", "IndexerSrc.txt"))));
        }

        [Test]
        public void GenerateProxyClass_Test()
        {
            Assert.That(GenerateProxyClass().NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText(Path.Combine("Proxy", "ClsSrc.txt"))));
        }

        [Test]
        public void GeneratedProxy_MethodOverrideTest()
        {
            IFoo<int> proxy = (IFoo<int>) GeneratedProxy<IFoo<int>, FooInterceptor>.Type.CreateInstance(new []{typeof(IFoo<int>)}, (object) null);

            string a, b = string.Empty;

            Assert.That(proxy.Prop, Is.EqualTo(1));
            Assert.That(proxy.Foo(0, out a, ref b), Is.EqualTo(1));
        }

        [Test]
        public void GeneratedProxy_IndexerTest()
        {
            IList<int>
                src = new List<int>(),
                proxy = (IList<int>) GeneratedProxy<IList<int>, ListInterceptor>.Type.CreateInstance(new[] { typeof(IList<int>) }, src);

            proxy.Add(1986);

            Assert.That(proxy.Count, Is.EqualTo(1));
            Assert.That(proxy[0], Is.EqualTo(1986));

            proxy[0]++;
            Assert.That(src[0], Is.EqualTo(1987));
        }

        [Test]
        public void CallTargetAndReturn_ShouldInvokeTheTargetMethod()
        {
            Assert.That(CallTargetAndReturn(Foo).NormalizeWhitespace().ToFullString(), Is.EqualTo("return Target.Foo(a, out b, ref c);"));
            Assert.That(CallTargetAndReturn(Bar).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo("{\n    Target.Bar();\n    return;\n}"));
        }

        [Test]
        public void ReadTargetAndReturn_ShouldReadTheGivenProperty()
        {
            Assert.That(ReadTargetAndReturn(Prop).NormalizeWhitespace().ToFullString(), Is.EqualTo("return Target.Prop;"));
            Assert.That(ReadTargetAndReturn(Indexer).NormalizeWhitespace().ToFullString(), Is.EqualTo("return Target[index];"));
        }

        [Test]
        public void WriteTarget_ShouldWriteTheGivenProperty()
        {
            Assert.That(WriteTarget(Prop).NormalizeWhitespace().ToFullString(), Is.EqualTo("Target.Prop = value;"));
        }

        [Test]
        public void ShouldCallTarget_ShouldCreateAnIfStatement()
        {
            Assert.That(ShouldCallTarget(DeclareLocal<object>("result"), SyntaxFactory.Block()).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo("if (result == CALL_TARGET)\n{\n}"));
        }

        private static MethodInfo GetMethod(string name) => typeof(IFoo<>).GetMethod(name);

        [Test]
        public void DeclareField_ShouldDeclareAField()
        {
            Assert.That(DeclareField<EventInfo>("FEvent", SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression), SyntaxKind.PrivateKeyword, SyntaxKind.StaticKeyword, SyntaxKind.ReadOnlyKeyword).NormalizeWhitespace().ToFullString(), Is.EqualTo("private static readonly System.Reflection.EventInfo FEvent = null;"));
        }

        [Test]
        public void DeclareEvent_ShouldDeclareTheDesiredEvent()
        {
            Assert.That(DeclareEvent(Event, SyntaxFactory.Block(), SyntaxFactory.Block()).NormalizeWhitespace(eol: "\n").ToString(), Is.EqualTo("event Solti.Utils.DI.Proxy.Tests.ProxyGeneratorTests.TestDelegate<System.Int32> Solti.Utils.DI.Proxy.Tests.ProxyGeneratorTests.IFoo<System.Int32>.Event\n{\n    add\n    {\n    }\n\n    remove\n    {\n    }\n}"));
        }

        [Test]
        public void GenerateProxyEvent_Test()
        {
            Assert.That(SyntaxFactory.ClassDeclaration("Test").WithMembers(SyntaxFactory.List(GenerateProxyEvent(Event))).NormalizeWhitespace(eol: "\n").ToString(), Is.EqualTo(File.ReadAllText(Path.Combine("Proxy", "EventSrc.txt"))));
        }

        [Test]
        public void DeclareMethod_ShouldSupportRefKeywords() 
        {
            Assert.That(DeclareMethod(typeof(IRefInterfacce).GetMethod(nameof(IRefInterfacce.RefMethod), BindingFlags.Instance | BindingFlags.Public)).NormalizeWhitespace().ToString(), Is.EqualTo("void Solti.Utils.DI.Proxy.Tests.ProxyGeneratorTests.IRefInterfacce.RefMethod(in System.Object a, out System.Object b, ref System.Object c)"));
        }

        [Test]
        public void InvokeMethod_ShouldSupportRefKeywords() 
        {
            Assert.That(InvokeMethod(typeof(IRefInterfacce).GetMethod(nameof(IRefInterfacce.RefMethod), BindingFlags.Instance | BindingFlags.Public), "target", new[] {"a", "b", "c"}).NormalizeWhitespace().ToString(), Is.EqualTo("target.RefMethod(in a, out b, ref c)"));
        }

        private interface IRefInterfacce 
        {
            void RefMethod(in object a, out object b, ref object c);
        }
    }

    internal class Cica<T> // NE nested legyen
    {
        public class Mica<TT>
        {
            public enum Hajj
            {
            }
        }
        public class Mica
        {
            public class Hajj<TT, TTT>
            {
            }
        }
    }
}