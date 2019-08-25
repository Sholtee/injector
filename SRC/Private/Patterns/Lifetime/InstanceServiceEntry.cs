/********************************************************************************
* InstanceServiceEntry.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    /// <summary>
    /// Describes an instance service entry.
    /// </summary>
    internal class InstanceServiceEntry : ServiceEntry
    {
        private readonly bool FReleaseOnDispose;
        private object FValue;

        public InstanceServiceEntry(Type @interface, object value, bool releaseOnDispose, ICollection<ServiceEntry> owner) : base(@interface, null, owner)
        {
            FValue = value;
            FReleaseOnDispose = releaseOnDispose;
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

        public override object Value => FValue;

        public override object GetService(IInjector injector, Type iface = null)
        {
            CheckDisposed();

            if (iface != null && iface != Interface) throw new NotSupportedException(Resources.NOT_SUPPORTED);
            return Value;
        }

        public override object Clone() => new InstanceServiceEntry(Interface, Value, releaseOnDispose: false /*csak egyszer szabaditsuk fel*/, owner: Owner);

        public override bool IsService => false;
        public override bool IsLazy => false;
        public override bool IsFactory => false;
        public override bool IsInstance => true;

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged && FReleaseOnDispose)
            {
                (FValue as IDisposable)?.Dispose();
                FValue = null;
            }
            base.Dispose(disposeManaged);
        }
    }
}