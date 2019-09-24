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
                //
                // Kell egyaltalan proxy-t letrhoznunk?
                //

                TInterface possibleResult = Target as TInterface;
                if (possibleResult != null)
                    return possibleResult;

                Type interfaceType = typeof(TInterface);

                if (!interfaceType.IsInterface())
                    throw new ArgumentException(Resources.NOT_AN_INTERFACE);

                if (interfaceType.ContainsGenericParameters())
                    throw new ArgumentException(Resources.CANT_INSTANTIATE_GENERICS);

                if (interfaceType.IsNotPublic())
                    throw new InvalidOperationException(string.Format(Resources.TYPE_NOT_VISIBLE, interfaceType));

                Type targetType = typeof(TTarget);

                if (targetType.IsNotPublic())
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
