﻿/********************************************************************************
* ServiceRequestVisitor.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

using NUnit.Framework;

namespace Solti.Utils.DI.Internals.Tests
{
    using Interfaces;

    [TestFixture]
    public class ServiceRequestVisitorTests
    {
        public static IEnumerable<Expression<Action<IInjector>>> Expressions
        {
            get
            {
                yield return i => i.Get(typeof(IList), "cica");
                yield return i => i.TryGet(typeof(IList), "cica");
                yield return i => i.Get<IList>("cica");
                yield return i => i.TryGet<IList>("cica");
            }
        }

        [Test]
        public void Visit_ShouldVisitAllTheInjectorInvocations([ValueSource(nameof(Expressions))] Expression<Action<IInjector>> expresision)
        {
            List<(Type Interface, string Name)> requests = new(); 

            ServiceRequestVisitor visitor = new((orig, _, iface, name) =>
            {
                requests.Add((iface, name));
                return orig;
            });
            visitor.Visit(expresision);

            Assert.That(requests.Count, Is.EqualTo(1));
            Assert.That(requests[0].Interface, Is.EqualTo(typeof(IList)));
            Assert.That(requests[0].Name, Is.EqualTo("cica"));
        }

        public static IEnumerable<Expression<Action<IInjector>>> ElaborateExpressions
        {
            get
            {
                yield return i => i.Get(typeof(IList), new string("cica"));
                yield return i => i.Get<IList>(new string("cica"));
            }
        }

        [Test]
        public void Visit_ShouldSkipInjectorInvocationsHavingNonConstantParameter([ValueSource(nameof(ElaborateExpressions))] Expression<Action<IInjector>> expresision)
        {
            List<(Type Interface, string Name)> requests = new();

            ServiceRequestVisitor visitor = new((orig, _, iface, name) =>
            {
                requests.Add((iface, name));
                return orig;
            });
            visitor.Visit(expresision);

            Assert.That(requests.Count, Is.EqualTo(0));
        }

        [Test]
        public void Visit_ShouldSupportNull()
        {
            List<(Type Interface, string Name)> requests = new();

            ServiceRequestVisitor visitor = new((orig, _, iface, name) =>
            {
                requests.Add((iface, name));
                return orig;
            });
            visitor.Visit((Expression<Action<IInjector>>) (i => i.Get(null, null)));

            Assert.That(requests.Count, Is.EqualTo(1));
            Assert.That(requests[0].Interface, Is.Null);
            Assert.That(requests[0].Name, Is.Null);
        }

        [Test]
        public void Visit_ShouldBeAbleToAmendTheExpression()
        {
            Expression<Action<IInjector>> expr = i => i.Get(typeof(IList), null);

            ServiceRequestVisitor visitor = new((orig, _, iface, name) => Expression.Throw(Expression.Constant(new Exception("cica"))));
            expr = (Expression<Action<IInjector>>) visitor.Visit(expr);

            Action<IInjector> fn = expr.Compile();
            Assert.Throws<Exception>(() => fn(null), "cica");
        }
    }
}
