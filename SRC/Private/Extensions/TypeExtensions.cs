/********************************************************************************
* TypeExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    internal static class TypeExtensions
    {
        public static bool IsInterfaceOf(this Type iface, Type implementation)
        {
            if (!iface.IsInterface || !implementation.IsClass) return false;

            //
            // Az IsAssignableFrom() csak nem generikus tipusokra mukodik (nem szamit
            // h a tipus mar tipizalva lett e v sem).
            //

            if (iface.IsAssignableFrom(implementation))
                return true;

            //
            // Innentol csak akkor kell tovabb mennunk ha mindket tipusunk generikus.
            //

            if (!iface.IsGenericType || !implementation.IsGenericType)
                return false;

            //
            // "List<> -> IList<>"
            //

            if (iface.IsGenericTypeDefinition && implementation.IsGenericTypeDefinition)
                return implementation.GetInterfaces().Where(i => i.IsGenericType).Any(i => i.GetGenericTypeDefinition() == iface);

            //
            // "List<T> -> IList<T>"
            //

            if (!iface.IsGenericTypeDefinition && !implementation.IsGenericTypeDefinition)
                return
                    iface.GetGenericArguments().SequenceEqual(implementation.GetGenericArguments()) &&
                    iface.GetGenericTypeDefinition().IsInterfaceOf(implementation.GetGenericTypeDefinition());

            //
            // "List<T> -> IList<>", "List<> -> IList<T>"
            //

            return false;
        }

        public static T CreateInstance<T>(this Type src, Type[] argTypes, params object[] args) // TODO: func cache
        {
            //
            // () => (T) new src((T_1) arg[0], (T_2) arg[1], ...)
            //

            return (T) Expression.Lambda<Func<object>>
            (
                Expression.New
                (
                    src.GetConstructor(argTypes), 
                    args.Select((arg, i) => Expression.Convert(Expression.Constant(arg), argTypes[i]))
                )
            ).Compile()();
        }
    }
}
