/********************************************************************************
* Visibility.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;
using System.Runtime.CompilerServices;

using NUnit.Framework;

[assembly: InternalsVisibleTo(Solti.Utils.DI.Internals.Tests.VisibilityTests.AnnotatedAssembly)]

namespace Solti.Utils.DI.Internals.Tests
{
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
    }
}
