/********************************************************************************
* InjectorDotNetLifetime.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal abstract class InjectorDotNetLifetime<TLifetime> : Lifetime, IHasPrecedence where TLifetime: InjectorDotNetLifetime<TLifetime>, new()
    {
        private PropertyInfo BoundProperty { get; }

        protected InjectorDotNetLifetime(Expression<Func<Lifetime>> bindTo, int precedence)
        {
            BoundProperty = (PropertyInfo) ((MemberExpression) bindTo.Body).Member;
            Precedence = precedence;
        }

        protected static void Bind()  // ModuleInitializer-bol kell hivni
        {
            var self = new TLifetime();
            self.BoundProperty.SetValue(null, self);
        }

        public int Precedence { get; }

        public override int CompareTo(Lifetime other) => other is IHasPrecedence hasPriority
            ? Precedence - hasPriority.Precedence
            : other.CompareTo(this) * -1;

        public override string ToString() => BoundProperty.Name;
    }
}
