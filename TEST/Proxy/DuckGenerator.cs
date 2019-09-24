/********************************************************************************
* DuckGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.IO;
using System.Reflection;

using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Solti.Utils.DI.Proxy.Tests
{
    using Internals;
    using Properties;

    [TestFixture]
    public sealed class DuckGeneratorTests
    {
        public delegate void TestDelegate<in T>(object sender, T eventArg);

        public interface IFoo<T>
        {
            int Foo<TT>(int a, out string b, ref TT c);
            void Bar();
            T Prop { get; set; }
           // event TestDelegate<T> Event;
        }

        private static readonly MethodInfo IfaceFoo = typeof(IFoo<int>).GetMethod("Foo", BindingFlags.Instance | BindingFlags.Public);

        private static readonly PropertyInfo IfaceProp = typeof(IFoo<int>).GetProperty("Prop", BindingFlags.Instance | BindingFlags.Public);

        public sealed class BadFoo
        {
            public int Foo<TT>(int a, out string b, TT c) => (b = string.Empty).GetHashCode();

            public int Prop { get; }
        }

        public sealed class GoodFoo<T>
        {
            public int Foo<TT>(int a, out string b, ref TT c) => (b = string.Empty).GetHashCode();

            public void Bar()
            {
            }

            public T Prop { get; set; }
        }

        [Test]
        public void GenerateDuckMethod_ShouldThrowIfMethodNotSupported()
        {
            Assert.Throws<MissingMethodException>
            (
                () => DuckGenerator<BadFoo, IFoo<int>>.GenerateDuckMethod(IfaceFoo),
                Resources.METHOD_NOT_SUPPORTED
            );
        }

        [Test]
        public void GenerateDuckMethod_ShouldGenerateTheDesiredMethodIfSupported()
        {
            Assert.That(DuckGenerator<GoodFoo<int>, IFoo<int>>.GenerateDuckMethod(IfaceFoo).NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Int32 Solti.Utils.DI.Proxy.Tests.DuckGeneratorTests.IFoo<System.Int32>.Foo<TT>(System.Int32 a, out System.String b, ref TT c) => Target.Foo(a, out b, ref c);"));
        }

        [Test]
        public void GenerateDuckProperty_ShouldThrowIfPropertyNotSupported()
        {
            //
            // Nincs setter.
            //

            Assert.Throws<MissingMethodException>
            (
                () => DuckGenerator<BadFoo, IFoo<int>>.GenerateDuckProperty(IfaceProp),
                Resources.PROPERTY_NOT_SUPPORTED
            );

            //
            // Van getter es setter is csak a tipus nem megfelelo.
            //

            Assert.Throws<MissingMethodException>
            (
                () => DuckGenerator<GoodFoo<string>, IFoo<int>>.GenerateDuckProperty(IfaceProp),
                Resources.PROPERTY_NOT_SUPPORTED
            );
        }


        [Test]
        public void GenerateDuckProperty_ShouldGenerateTheDesiredPropertyIfSupported()
        {
            Assert.That(DuckGenerator<GoodFoo<int>, IFoo<int>>.GenerateDuckProperty(IfaceProp).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo("System.Int32 Solti.Utils.DI.Proxy.Tests.DuckGeneratorTests.IFoo<System.Int32>.Prop\n{\n    get => Target.Prop;\n    set => Target.Prop = value;\n}"));
        }

        private class Dummy
        {
        }

        [Test]
        public void GenerateDuckClass_ShouldThrowOnMissingImplementation()
        {
            var ex = Assert.Throws<AggregateException>(() => DuckGenerator<Dummy, IFoo<int>>.GenerateDuckClass());
            Assert.That(ex.InnerExceptions.Count, Is.EqualTo(3));
        }

        [Test]
        public void GenerateDuckClass_ShouldGenerateTheDesiredClass()
        {
            Assert.That(DuckGenerator<GoodFoo<int>, IFoo<int>>.GenerateDuckClass().NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo(File.ReadAllText(Path.Combine("Proxy", "DuckClsSrc.txt"))));
        }

        [Test]
        public void GeneratedDuck_Test()
        {
            var target = new GoodFoo<int>();

            IFoo<int> proxy = (IFoo<int>) GeneratedDuck<IFoo<int>, GoodFoo<int>>.Type.CreateInstance(new[] { typeof(GoodFoo<int>) }, target);

            proxy.Prop = 1;
            Assert.That(target.Prop, Is.EqualTo(1));

            string a, b = String.Empty;

            Assert.That(proxy.Foo(0, out a, ref b), Is.EqualTo(string.Empty.GetHashCode()));
        }
    }
}
