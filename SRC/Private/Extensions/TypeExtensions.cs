/********************************************************************************
* TypeExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Solti.Utils.DI.Internals
{
    using Properties;
    using Annotations;

    internal static partial class TypeExtensions
    {
        public static bool IsInterfaceOf(this Type iface, Type implementation)
        {
            if (!iface.IsInterface() || !implementation.IsClass()) return false;

            //
            // Az IsAssignableFrom() csak nem generikus tipusokra mukodik (nem szamit
            // h a tipus mar tipizalva lett e v sem).
            //

            if (iface.IsAssignableFrom(implementation))
                return true;

            //
            // Innentol csak akkor kell tovabb mennunk ha mindket tipusunk generikus.
            //

            if (!iface.IsGenericType() || !implementation.IsGenericType())
                return false;

            //
            // "List<> -> IList<>"
            //

            if (iface.IsGenericTypeDefinition() && implementation.IsGenericTypeDefinition())
                return implementation.GetInterfaces().Where(i => i.IsGenericType()).Any(i => i.GetGenericTypeDefinition() == iface);

            //
            // "List<T> -> IList<T>"
            //

            if (!iface.IsGenericTypeDefinition() && !implementation.IsGenericTypeDefinition())
                return
                    iface.GetGenericArguments().SequenceEqual(implementation.GetGenericArguments()) &&
                    iface.GetGenericTypeDefinition().IsInterfaceOf(implementation.GetGenericTypeDefinition());

            //
            // "List<T> -> IList<>", "List<> -> IList<T>"
            //

            return false;
        }

        public static ConstructorInfo GetApplicableConstructor(this Type src) => Cache<Type, ConstructorInfo>.GetOrAdd(src, () =>
        {
            //
            // Az implementacionak pontosan egy (megjelolt) konstruktoranak kell lennie.
            //

            try
            {
                IReadOnlyList<ConstructorInfo> constructors = src.GetConstructors();

                return constructors.SingleOrDefault(ctor => ctor.GetCustomAttribute<ServiceActivatorAttribute>() != null) ?? constructors.Single();
            }
            catch (InvalidOperationException)
            {
                throw new NotSupportedException(string.Format(Resources.CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED, src));
            }
        });

        public static object CreateInstance(this Type src, Type[] argTypes, params object[] args)
        {
            ConstructorInfo ctor = src.GetConstructor(argTypes);
            if (ctor == null)
                throw new ArgumentException(Resources.CONSTRUCTOR_NOT_FOUND, nameof(argTypes));

            return ctor.Call(args);
        }

        public static IReadOnlyList<Type> GetOwnGenericArguments(this Type src) => src
            .GetGenericArguments()
            .Except(src.DeclaringType?.GetGenericArguments() ?? new Type[0], new GenericArgumentComparer())
            .ToArray();

        //
        // Sajat comparer azert kell mert pl List<T> es IList<T> eseten typeof(List<T>).GetGenericArguments[0] != typeof(IList<T>).GetGenericArguments[0] 
        // testzoleges "T"-re.
        //

        private sealed class GenericArgumentComparer : IEqualityComparer<Type>
        {
            public bool Equals(Type x, Type y) => GetHashCode(x) == GetHashCode(y);

            //
            // Generikus argumentumnak nincs teljes neve ezert a lenti sor jol kezeli a fenti
            // problemat.
            //

            public int GetHashCode(Type obj) => (obj.FullName ?? obj.Name).GetHashCode();
        }

        public static IReadOnlyList<Type> GetParents(this Type type)
        {
            return GetParentsInternal().Reverse().ToArray();

            IEnumerable<Type> GetParentsInternal()
            {
                for (Type parent = type.DeclaringType; parent != null; parent = parent.DeclaringType)
                    yield return parent;
            }
        }

        //
        // "&":  referencia szerinti parameter
        // "`d": generikus tipus ahol "d" egesz szam
        // "[T, TT]": generikus parameterek
        //

        private static readonly Regex TypeNameReplacer = new Regex(@"\&|`\d+(\[[\w,]+\])?", RegexOptions.Compiled);

        public static string GetFriendlyName(this Type src)
        {
            Debug.Assert(!src.IsGenericType() || src.IsGenericTypeDefinition());
            return TypeNameReplacer.Replace(src.IsNested ? src.Name : src.ToString(), string.Empty);
        }

        public static IEnumerable<TMember> ListMembers<TMember>(this Type src, Func<Type, BindingFlags, TMember[]> factory) where TMember : MemberInfo
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            if (src.IsInterface())
                //
                // Az BindingFlags.FlattenHierarchy interface-ekre nem mukodik.
                //

                return factory(src, flags)
                    .Concat
                    (
                        src.GetInterfaces().SelectMany(iface => ListMembers(iface, factory))
                    )
                    .Distinct();

            return factory(src, flags | BindingFlags.FlattenHierarchy);
        }

        public static IEnumerable<Assembly> GetReferences(this Type src)
        {
            Assembly declaringAsm = src.Assembly();

            var references = new[] { declaringAsm }.Concat(declaringAsm.GetReferences());

            //
            // Generikus parameterek szerepelhetnek masik szerelvenyben.
            //

            if (src.IsGenericType())
                foreach (Type type in src.GetGenericArguments().Where(t => !t.IsGenericParameter))
                    references = references.Concat(type.GetReferences());

            return references.Distinct();
        }
    }
}
