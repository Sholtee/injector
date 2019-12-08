/********************************************************************************
* InstanceServiceEntry.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Globalization;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    /// <summary>
    /// Describes an instance service entry.
    /// </summary>
    internal class InstanceServiceEntry : AbstractServiceEntry
    {
        #region InstanceServiceReference
        private sealed class InstanceServiceReference : ServiceReference // hacky
        {
            public InstanceServiceReference(Type @interface, string name, bool releaseOnDispose) : base(@interface, name)
            {
                ReleaseOnDispose = releaseOnDispose;        
            }

            public bool ReleaseOnDispose { get; }

            protected override void Dispose(bool disposeManaged)
            {
                if (disposeManaged && !ReleaseOnDispose) Instance = null; 
                base.Dispose(disposeManaged);
            }
        }
        #endregion

        private readonly ServiceReference FService;

        public InstanceServiceEntry(Type @interface, string name, object instance, bool releaseOnDispose, IServiceContainer owner) : base(@interface, name, null, owner)
        {
            //
            // Interface-t nem kell ellenorizni (az os megteszi).
            //

            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            if (!@interface.IsInstanceOfType(instance))
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.NOT_ASSIGNABLE, @interface, instance.GetType()));

            FService = new InstanceServiceReference(@interface, name, releaseOnDispose) { Instance = instance };
        }

        public override Type Implementation => null;

        public override Func<IInjector, Type, object> Factory
        {
            //
            // Nem kell lekerdezeskor kivetelt dobni (Proxy() hivaskor ha a Factory null ugy is
            // hiba van).
            //

            get => null;
            set => throw new InvalidOperationException();
        }

        public override object Value => FService.Instance;

        public override void GetService(Func<IInjector> injectorFactory, ref ServiceReference reference)
        {
            CheckDisposed();

            reference = FService;
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged) FService.Release();
            base.Dispose(disposeManaged);
        }
    }
}