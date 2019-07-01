/********************************************************************************
* ServiceEntry.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
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

        public bool IsInstance => !IsService && !IsFactory && Value != null;
        #endregion

        #region Mutables
        /// <summary>
        /// Peldany gyar. Generikus implementacional (nem feltetlen) es Instance() hivasnal (biztosan) NULL.
        /// </summary>
        public Func<IInjector, Type, object> Factory { get; set; }

        /// <summary>
        /// Legyartott (Lifetime.Singleton eseten) vagy kivulrol definialt (Instance() hivas) peldany, kulomben NULL. 
        /// </summary>
        public object Value
        {
            get => FValue;
            set => FValue = FValue == null ? value : throw new InvalidOperationException(Resources.MULTIPLE_ASSIGN);
        }
        #endregion

        private ServiceEntry(Type @interface, object implementation, Lifetime? lifetime, EntryAttributes attributes)
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
            (object) implementation, 
            lifetime,
            ShouldRelease(lifetime) ? EntryAttributes.ReleaseOnDispose : EntryAttributes.None
        ) {}

        public ServiceEntry(Type @interface, Lifetime lifetime, ITypeResolver resolver): this
        (
            @interface, 
            new Lazy<Type>(() => resolver.Resolve(@interface), LazyThreadSafetyMode.ExecutionAndPublication), 
            lifetime,
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

            if (Value is IInjector)
                //
                // Ide csak akkor jutunk ha Injector peldanyt akarnank szulo kontenerkent hasznalni.
                //

                throw new InvalidOperationException(Resources.CANT_CLONE);

            //
            // Instance() hivassal felvett ertek felszabaditasa mindig csak a abban a bejegyzesben 
            // tortenik ahol az ertek eloszor definialva lett.
            //

            return new ServiceEntry(Interface, FImplementation, Lifetime, IsInstance ? FAttributes & ~EntryAttributes.ReleaseOnDispose : FAttributes)
            {
                Factory = Factory,
                Value   = Value
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

                FValue          == that.FValue    &&
                FImplementation == that.FImplementation;
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