/********************************************************************************
* InjectorDotNetLifetime.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal abstract class InjectorDotNetLifetime : Lifetime, IHasPrecedence
    {
        private PropertyInfo BoundProperty { get; }

        protected InjectorDotNetLifetime(Expression<Func<Lifetime>> bindTo, int precedence)
        {
            BoundProperty = (PropertyInfo) ((MemberExpression) bindTo.Body).Member;
            Precedence = precedence;
        }

        [ModuleInitializer]
        public static void Initialize() // nem lehet generikusban
        {
            foreach (Type t in typeof(InjectorDotNetLifetime).Assembly.DefinedTypes.Where(t => t.GetInterfaces().Any(iface => iface.GUID == typeof(IConcreteLifetime<>).GUID)))
            {
                var lifetime = (InjectorDotNetLifetime) Activator.CreateInstance(t);
                lifetime.BoundProperty.SetValue(null, lifetime);
            }
        }

        public int Precedence { get; }

        public override int CompareTo(Lifetime other) => other is IHasPrecedence hasPrecedence
            ? Precedence - hasPrecedence.Precedence
            : other.CompareTo(this) * -1;

        public override string ToString() => BoundProperty.Name;
    }
}
