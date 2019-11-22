﻿/********************************************************************************
* TypeExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    [TestFixture]
    public sealed class TypeExtensionsTests
    {
        [Test]
        public void ListMembers_ShouldReturnOverLoadedMembers() 
        {
            MethodInfo[] methods = typeof(IEnumerable<string>).ListMembers(System.Reflection.TypeExtensions.GetMethods).ToArray();
            Assert.That(methods.Length, Is.EqualTo(2));
            Assert.That(methods.Contains(((MethodCallExpression) ((Expression<Action<IEnumerable<string>>>) (i => i.GetEnumerator())).Body).Method));
            Assert.That(methods.Contains(((MethodCallExpression) ((Expression<Action<System.Collections.IEnumerable>>)(i => i.GetEnumerator())).Body).Method));

            PropertyInfo[] properties = typeof(IEnumerator<string>).ListMembers(System.Reflection.TypeExtensions.GetProperties).ToArray();
            Assert.That(properties.Length, Is.EqualTo(2));
            Assert.That(properties.Contains((PropertyInfo) ((MemberExpression) ((Expression<Func<IEnumerator<string>, string>>)(i => i.Current)).Body).Member));
            Assert.That(properties.Contains((PropertyInfo) ((MemberExpression) ((Expression<Func<System.Collections.IEnumerator, object>>)(i => i.Current)).Body).Member));
        }
    }
}
