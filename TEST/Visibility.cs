/********************************************************************************
* Visibility.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

using NUnit.Framework;

[assembly: InternalsVisibleTo(Solti.Utils.DI.Internals.Tests.VisibilityTests.AnnotatedAssembly)]

namespace Solti.Utils.DI.Internals.Tests
{
    using Properties;

    [TestFixture]
    public class VisibilityTests
    {
        public const string
            NonAnnotatedAssembly = nameof(NonAnnotatedAssembly),
            AnnotatedAssembly = nameof(AnnotatedAssembly);

        [Test]
        public void GrantedFor_ShouldReflectTheVisibilityOfType()
        {
            Assert.That(Visibility.GrantedFor(typeof(IInterface), NonAnnotatedAssembly), Is.False);
            Assert.That(Visibility.GrantedFor(typeof(IInterface), AnnotatedAssembly), Is.True);
            Assert.That(Visibility.GrantedFor(typeof(PublicClassWithInternalMethodAndNestedType.InternalNestedClass), NonAnnotatedAssembly), Is.False);
            Assert.That(Visibility.GrantedFor(typeof(PublicClassWithInternalMethodAndNestedType.InternalNestedClass), AnnotatedAssembly), Is.True);
            Assert.That(Visibility.GrantedFor(typeof(PrivateClass), NonAnnotatedAssembly), Is.False);
            Assert.That(Visibility.GrantedFor(typeof(PrivateClass), AnnotatedAssembly), Is.False);
        }

        //[Test]
        public void GrantedFor_ShouldReflectTheVisibilityOfMethod()
        {
            Assert.That(Visibility.GrantedFor(typeof(PublicClassWithInternalMethodAndNestedType).GetMethod(nameof(PublicClassWithInternalMethodAndNestedType.Foo), BindingFlags.Instance | BindingFlags.NonPublic), NonAnnotatedAssembly), Is.False);
            Assert.That(Visibility.GrantedFor(typeof(PublicClassWithInternalMethodAndNestedType).GetMethod(nameof(PublicClassWithInternalMethodAndNestedType.Foo), BindingFlags.Instance | BindingFlags.NonPublic), AnnotatedAssembly), Is.True);
            Assert.That(Visibility.GrantedFor(typeof(PublicClassWithInternalMethodAndNestedType.InternalNestedClass).GetMethod(nameof(PublicClassWithInternalMethodAndNestedType.InternalNestedClass.Bar), BindingFlags.Instance | BindingFlags.Public), NonAnnotatedAssembly), Is.False);
            Assert.That(Visibility.GrantedFor(typeof(PublicClassWithInternalMethodAndNestedType.InternalNestedClass).GetMethod(nameof(PublicClassWithInternalMethodAndNestedType.InternalNestedClass.Bar), BindingFlags.Instance | BindingFlags.Public), AnnotatedAssembly), Is.True);
            Assert.That(Visibility.GrantedFor(typeof(PrivateClass).GetMethod(nameof(PrivateClass.Baz), BindingFlags.Instance | BindingFlags.Public), NonAnnotatedAssembly), Is.False);
            Assert.That(Visibility.GrantedFor(typeof(PrivateClass).GetMethod(nameof(PrivateClass.Baz), BindingFlags.Instance | BindingFlags.Public), AnnotatedAssembly), Is.False);
        }

        internal interface IInterface { }

        public class PublicClassWithInternalMethodAndNestedType
        {
            internal void Foo() { }

            internal class InternalNestedClass
            {
                public void Bar() { }
            }
        }

        private class PrivateClass
        {
            public void Baz() { }
        }

        [Test]
        public void Check_ShouldThrowIfTheMemberNotVisible()
        {
            Assert.Throws<Exception>(() => Visibility.Check(typeof(TestClass).GetMethod(nameof(TestClass.InternalMethod), BindingFlags.Instance | BindingFlags.NonPublic), NonAnnotatedAssembly), Resources.IVT_REQUIRED);
            Assert.DoesNotThrow(() => Visibility.Check(typeof(TestClass).GetMethod(nameof(TestClass.InternalMethod), BindingFlags.Instance | BindingFlags.NonPublic), AnnotatedAssembly));

            Assert.DoesNotThrow(() => Visibility.Check(typeof(TestClass).GetProperty(nameof(TestClass.InternalProtectedProperty)), NonAnnotatedAssembly, checkGet: true, checkSet: false));
            Assert.Throws<Exception>(() => Visibility.Check(typeof(TestClass).GetProperty(nameof(TestClass.InternalProtectedProperty)), NonAnnotatedAssembly, checkGet: false, checkSet: true), Resources.IVT_REQUIRED);
            Assert.DoesNotThrow(() => Visibility.Check(typeof(TestClass).GetProperty(nameof(TestClass.InternalProtectedProperty)), AnnotatedAssembly, checkGet: false, checkSet: true));

            Assert.DoesNotThrow(() => Visibility.Check(typeof(TestClass).GetEvent(nameof(TestClass.PublicEvent)), NonAnnotatedAssembly, checkAdd: true, checkRemove: true));

            Assert.Throws<Exception>(() => Visibility.Check(typeof(TestClass).GetMethod("ProtectedMethod", BindingFlags.Instance | BindingFlags.NonPublic), NonAnnotatedAssembly), Resources.METHOD_NOT_VISIBLE);
            Assert.Throws<Exception>(() => Visibility.Check(typeof(TestClass).GetMethod("ProtectedMethod", BindingFlags.Instance | BindingFlags.NonPublic), AnnotatedAssembly), Resources.METHOD_NOT_VISIBLE);

            Assert.Throws<Exception>(() => Visibility.Check(typeof(TestClass).GetProperty("PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic), NonAnnotatedAssembly, checkGet: true, checkSet: false), Resources.METHOD_NOT_VISIBLE);
            Assert.Throws<Exception>(() => Visibility.Check(typeof(TestClass).GetProperty("PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic), NonAnnotatedAssembly, checkGet: false, checkSet: true), Resources.METHOD_NOT_VISIBLE);
            Assert.Throws<Exception>(() => Visibility.Check(typeof(TestClass).GetProperty("PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic), AnnotatedAssembly, checkGet: true, checkSet: false), Resources.METHOD_NOT_VISIBLE);
            Assert.Throws<Exception>(() => Visibility.Check(typeof(TestClass).GetProperty("PrivateProperty", BindingFlags.Instance | BindingFlags.NonPublic), AnnotatedAssembly, checkGet: false, checkSet: true), Resources.METHOD_NOT_VISIBLE);
        }

        public class TestClass 
        {
            internal void InternalMethod() { }
            public int InternalProtectedProperty { get; internal protected set; }
            public event EventHandler PublicEvent;
            protected void ProtectedMethod() { }
            private int PrivateProperty { get; set; }
        }
    }
}
