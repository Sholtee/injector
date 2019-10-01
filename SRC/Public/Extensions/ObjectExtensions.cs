/********************************************************************************
* ObjectExtensions.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Proxy
{
    using Internals;

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

            /// <summary>
            /// Creates a new <see cref="DuckFactory{TTarget}"/> instance against the given <paramref name="target"/>.
            /// </summary>
            /// <param name="target">The target of this factory.</param>
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
