/********************************************************************************
* InstanceServiceEntry.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    /// <summary>
    /// Describes an instance service entry.
    /// </summary>
    internal class InstanceServiceEntry : AbstractServiceEntry
    {
        private readonly bool FReleaseOnDispose;
        private object FValue;

        public InstanceServiceEntry(Type @interface, string name, object value, bool releaseOnDispose, IServiceContainer owner) : base(@interface, name, null, owner)
        {
            FValue = value;
            FReleaseOnDispose = releaseOnDispose;
        }

        public override Type Implementation => null;

        public override object UserData => null;

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