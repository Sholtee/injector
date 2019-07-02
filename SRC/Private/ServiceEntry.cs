/********************************************************************************
* ServiceEntry.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    /// <summary>
    /// Stores a service definition.
    /// </summary>
    /// <remarks>This is an internal class so it may change from version to version. Don't use it!</remarks>
    public sealed class ServiceEntry: Disposable, IServiceInfo, ICloneable // TODO: !!TESTS!!
    {
        private readonly object FImplementation;
        private object FValue;
        private readonly EntryAttributes FAttributes;

        [Flags]
        private enum EntryAttributes
        {
            None = 0,
            ReleaseOnDispose
        }

        #region Immutables
        public Type Interface { get; }

        public Type Implementation => (FImplementation as Lazy<Type>)?.Value ?? (Type) FImplementation;

        public Lifetime? Lifetime { get; }

        public bool IsService => FImplementation != null;

        public bool IsLazy => FImplementation is Lazy<Type>;

        public bool IsFactory => !IsService && Factory != null;

        public bool IsInstance => Lifetime == null && Value != null;
        #endregion

        #region Mutables
        /// <summary>
        /// See <see cref="IServiceInfo"/>.
        /// </summary>
        public Func<IInjector, Type, object> Factory { get; set; }

        /// <summary>
        /// See <see cref="IServiceInfo"/>.
        /// </summary>
        public object Value
        {
            get => FValue;
            set => FValue = FValue == null ? value : throw new InvalidOperationException(Resources.MULTIPLE_ASSIGN);
        }
        #endregion

        private ServiceEntry(Type @interface, Lifetime? lifetime, object implementation, EntryAttributes attributes)
        {
            Interface = @interface;
            Lifetime  = lifetime;

            FImplementation = implementation;
            FAttributes     = attributes;
        }

        private static bool ShouldRelease(Lifetime lifetime) => new []{ DI.Lifetime.Singleton }.Contains(lifetime);

        public ServiceEntry(Type @interface, Lifetime lifetime, Type implementation = null): this
        (
            @interface,
            lifetime,
            (object) implementation,         
            ShouldRelease(lifetime) ? EntryAttributes.ReleaseOnDispose : EntryAttributes.None
        ) {}

        public ServiceEntry(Type @interface, Lifetime lifetime, ITypeResolver resolver): this
        (
            @interface,
            lifetime,
            new Lazy<Type>(() => resolver.Resolve(@interface), LazyThreadSafetyMode.ExecutionAndPublication),            
            ShouldRelease(lifetime) ? EntryAttributes.ReleaseOnDispose : EntryAttributes.None
        ) {}

        public ServiceEntry(Type @interface, object value, bool releaseOnDispose) : this(@interface, null, null, releaseOnDispose ? EntryAttributes.ReleaseOnDispose : EntryAttributes.None)
        {
            Value = value;
        }

        /// <summary>
        /// See <see cref="ICloneable"/>.
        /// </summary>
        public object Clone()
        {
            CheckDisposed();

            Debug.Assert(!(Value is IInjector));

            //
            // Instance() hivassal felvett ertek felszabaditasa mindig csak a abban a bejegyzesben 
            // tortenik ahol az ertek eloszor definialva lett.
            //

            bool shouldCopyValue = Lifetime == null;

            return new ServiceEntry(Interface, Lifetime, FImplementation, shouldCopyValue ? FAttributes & ~EntryAttributes.ReleaseOnDispose : FAttributes)
            {
                Factory = Factory,
                Value   = shouldCopyValue ? Value : null
            };
        }

        /// <summary>
        /// See <see cref="object.Equals(object)"/>
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;

            ServiceEntry that = obj as ServiceEntry;

            return
                that != null &&

                //
                // Ket szerviz bejegyzest akkor tekintunk egyenlonek ha az FAttributes kivetelevel (mivel
                // klonozaskor az valtozhat) minden mas mezoje egyenlo.
                //

                Interface       == that.Interface &&
                Lifetime        == that.Lifetime  &&
                Factory         == that.Factory   &&
                Value           == that.Value     && 
                
                FImplementation == that.FImplementation; // nem triggereli a resolver-t
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                if (FAttributes.HasFlag(EntryAttributes.ReleaseOnDispose))
                {
                    (Value as IDisposable)?.Dispose();
                }
            }

            base.Dispose(disposeManaged);
        }
    }
}