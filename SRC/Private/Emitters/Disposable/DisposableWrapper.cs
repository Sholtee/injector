/********************************************************************************
* DisposableWrapper.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Reflection;

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
        public DisposableWrapper(T target) 
        {
            //
            // Ne "T"-n legyen megszigoritas h IDiposable leszarmazott legyen mert T lehet interface ami nem
            // leszarmazott, viszont az implementacio ettol meg lehet az.
            //

            Debug.Assert(typeof(IDisposable).IsInstanceOfType(target));
            Target = target;
        }

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
                ((IDisposable) Target).Dispose();
                Target = default;
            }

            base.Dispose(disposeManaged);
        }
    }

    internal static class DisposableWrapper 
    {
        public static IDisposableEx Create(Type iface, IDisposable instance) 
        {
            Debug.Assert(iface.IsInstanceOfType(instance));

            Type wrapper = Cache<Type, Type>.GetOrAdd
            (
                iface,
                () => (TypeGenerator) typeof(GeneratedDisposable<>).MakeGenericType(iface).CreateInstance(Array.Empty<Type>())          
            );

            return (IDisposableEx) wrapper.CreateInstance(new[] { iface }, instance);
        }        
    }
}
