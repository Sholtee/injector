/********************************************************************************
* DuckGenerator.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;

using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Solti.Utils.DI.Duck.Tests
{
    using Internals;
    using Properties;

    [TestFixture]
    public sealed class DuckGeneratorTests
    {
        internal delegate void TestDelegate<in T>(object sender, T eventArg);

        internal interface IFoo<T>
        {
            int Foo<TT>(int a, out string b, ref TT c);
            void Bar();
            T Prop { get; set; }
            event TestDelegate<T> Event;
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
            public T Prop { get; set; }
        }

        [Test]
        public void GenerateDuckMethod_ShouldThrowIfMethodNotSupported()
        {
            var generator = new DuckGenerator(typeof(BadFoo));

            Assert.Throws<MissingMethodException>
            (
                () => generator.GenerateDuckMethod(IfaceFoo),
                Resources.METHOD_NOT_SUPPORTED
            );
        }

        [Test]
        public void GenerateDuckMethod_ShouldGenerateTheDesiredMethodIfSupported()
        {
            var generator = new DuckGenerator(typeof(GoodFoo<>));

            Assert.That(generator.GenerateDuckMethod(IfaceFoo).NormalizeWhitespace().ToFullString(), Is.EqualTo("System.Int32 Solti.Utils.DI.Duck.Tests.DuckGeneratorTests.IFoo<System.Int32>.Foo<TT>(System.Int32 a, out System.String b, ref TT c) => Target.Foo(a, out b, ref c)"));
        }

        [Test]
        public void GenerateDuckProperty_ShouldThrowIfPropertyNotSupported()
        {
            //
            // Nincs setter.
            //

            var generator = new DuckGenerator(typeof(BadFoo));

            Assert.Throws<MissingMethodException>
            (
                () => generator.GenerateDuckProperty(IfaceProp),
                Resources.PROPERTY_NOT_SUPPORTED
            );

            //
            // Van getter setter is csak a tipus nem megfelelo.
            //

            generator = new DuckGenerator(typeof(GoodFoo<string>));

            Assert.Throws<MissingMethodException>
            (
                () => generator.GenerateDuckProperty(IfaceProp),
                Resources.PROPERTY_NOT_SUPPORTED
            );
        }


        [Test]
        public void GenerateDuckProperty_ShouldGenerateTheDesiredPropertyIfSupported()
        {
            var generator = new DuckGenerator(typeof(GoodFoo<int>));

            Assert.That(generator.GenerateDuckProperty(IfaceProp).NormalizeWhitespace(eol: "\n").ToFullString(), Is.EqualTo("System.Int32 Solti.Utils.DI.Duck.Tests.DuckGeneratorTests.IFoo<System.Int32>.Prop\n{\n    get => Target.Prop;\n    set => Target.Prop = value;\n}"));
        }
    }
}
