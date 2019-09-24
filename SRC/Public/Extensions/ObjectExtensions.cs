/********************************************************************************
* ObjectExtensions.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Proxy
{
    using Internals;
    using Properties;

    /// <summary>
    /// Defines the base class for duck typing.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Generates duck typing proxy objects.
        /// </summary>
        public class DuckFactory<TTarget>
        {
            internal TTarget Target { get; }

            public DuckFactory(TTarget target) => Target = target;

            /// <summary>
            /// Generates a proxy object to let the target behave like a <typeparamref name="TInterface"/> instance.
            /// </summary>
            /// <returns>The newly created proxy object.</returns>
            public TInterface Like<TInterface>() where TInterface: class
            {
                Type interfaceType = typeof(TInterface);

                if (!interfaceType.IsInterface())
                    throw new ArgumentException(Resources.NOT_AN_INTERFACE);

                //
                // Kell egyaltalan proxy-t letrhoznunk?
                //

                TInterface possibleResult = Target as TInterface;
                if (possibleResult != null)
                    return possibleResult;

                //
                // A generalt tipus meg fogja valositani az interface-t -> lathato kell legyen.
                //
                // Megjegyzendo h NE a IsNotPublic()-ot hivjuk a tipuson mert az internal lathatosagra
                // hamissal fog visszaterni.
                //

                if (!interfaceType.IsPublic() && !interfaceType.IsNestedPublic())
                    throw new InvalidOperationException(string.Format(Resources.TYPE_NOT_VISIBLE, interfaceType));

                Type targetType = typeof(TTarget);

                //
                // A generalt proxy konstruktora parameterkent varja a target-et.
                //

                if (!targetType.IsPublic() && !targetType.IsNestedPublic())
                    throw new InvalidOperationException(string.Format(Resources.TYPE_NOT_VISIBLE, targetType));

                return (TInterface) GeneratedDuck<TInterface, TTarget>
                    .Type
                    .CreateInstance(new[] { typeof(TTarget) }, Target);
            }
        }

        /// <summary>
        /// Marks a <typeparamref name="TTarget"/> instance for duck typing.
        /// </summary>
        public static DuckFactory<TTarget> Act<TTarget>(this TTarget target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            return new DuckFactory<TTarget>(target);
        }
    }
}
