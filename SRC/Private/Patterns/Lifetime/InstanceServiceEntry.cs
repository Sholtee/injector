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
        private readonly bool FReleaseOnDispose;
        private object FInstance;

        public InstanceServiceEntry(Type @interface, string name, object instance, bool releaseOnDispose, IServiceContainer owner) : base(@interface, name, null, owner)
        {
            //
            // Interface-t nem kell ellenorizni (az os megteszi).
            //

            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            if (!@interface.IsInstanceOfType(instance))
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Resources.NOT_ASSIGNABLE, @interface, instance.GetType()));

            FInstance = instance;
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

        public override object Value => FInstance;

        public override object GetService(Func<IInjector> injectorFactory)
        {
            CheckDisposed();

            return Value;
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged && FReleaseOnDispose)
            {
                (FInstance as IDisposable)?.Dispose();
                FInstance = null;
            }
            base.Dispose(disposeManaged);
        }
    }
}