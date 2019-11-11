/********************************************************************************
* DisposableWrapper.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Defines the base class for wrapping disposable objects.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    public abstract class DisposableWrapper<T> : Disposable, IHasTarget<T>
    {
        /// <summary>
        /// Creates a new <see cref="DisposableWrapper{T}"/> instance.
        /// </summary>
        /// <param name="target">The target of this instance.</param>
        public DisposableWrapper(T target) => Target = target;

        /// <summary>
        /// The target of this instance.
        /// </summary>
        public T Target { get; private set; }

        /// <summary>
        /// See <see cref="Disposable"/>.
        /// </summary>
        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                //
                // Ne "T"-n legyen megszigoritas h IDiposable leszarmazott legyen mert T lehet interface ami nem
                // leszarmazott, viszont az implementacio ettol meg lehet IDisposable.
                //

                (Target as IDisposable)?.Dispose();
                Target = default;
            }

            base.Dispose(disposeManaged);
        }
    }

    internal static class DisposableWrapper 
    {
        public static IDisposableEx Create(Type type, IDisposable instance) 
        {
            Type wrapper = Cache<Type, Type>.GetOrAdd(type, () =>
            {
                var gen = (ITypeGenerator) typeof(GeneratedDisposable<>).MakeGenericType(type).CreateInstance(Array.Empty<Type>());
                return gen.Type;
            });

            return (IDisposableEx) wrapper.CreateInstance(new[] { type }, instance);
        }        
    }
}
