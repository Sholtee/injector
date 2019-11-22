/********************************************************************************
* DisposableWrapper.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Proxy;

    /// <summary>
    /// Defines the base class for wrapping disposable objects.
    /// </summary>
    /// <typeparam name="TInterface">The target type.</typeparam>
    [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "The pattern implemented correctly.")]
    public abstract class DisposableWrapper<TInterface> : InterfaceInterceptor<TInterface>, IDisposableEx where TInterface : class
    {
        /// <summary>
        /// Creates a new <see cref="DisposableWrapper"/> instace.
        /// </summary>
        /// <param name="target">The target of this instace.</param>
        public DisposableWrapper(TInterface target) : base(target)
        {
            Debug.Assert(target is IDisposable);
        }

        private static readonly MethodInfo IfaceDispose = ((MethodCallExpression) ((Expression<Action<IDisposable>>) (d => d.Dispose())).Body).Method;

        /// <summary>
        /// See <see cref="InterfaceInterceptor{TInterface}"/>.
        /// </summary>
        public override object Invoke(MethodInfo method, object[] args, MemberInfo extra)
        {
            if (method == IfaceDispose) 
            {
                Dispose();
                return null;
            }

            return base.Invoke(method, args, extra);
        }

        /// <summary>
        /// Indicates that the current instance was disposed or not.
        /// </summary>
        public bool Disposed { get; private set; }

        /// <summary>
        /// See <see cref="IDisposable"/>.
        /// </summary>
        public void Dispose()
        {
            ((IDisposable) Target).Dispose();
            GC.SuppressFinalize(this);
            Disposed = true;
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
                () => ProxyFactory.GetGeneratedProxyType(iface, typeof(DisposableWrapper<>).MakeGenericType(iface))
            );

            return (IDisposableEx) wrapper.CreateInstance(new[] { iface }, instance);
        }        
    }
}
