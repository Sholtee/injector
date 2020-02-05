/********************************************************************************
* InstanceServiceEntry.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    /// <summary>
    /// Describes an instance service entry.
    /// </summary>
    internal class InstanceServiceEntry : AbstractServiceEntry
    {
        private readonly AbstractServiceReference FService;

        public InstanceServiceEntry(Type @interface, string name, object instance, bool releaseOnDispose, IServiceContainer owner) : base(@interface, name, null, owner ?? throw new ArgumentNullException(nameof(owner)))
        {
            //
            // Interface-t nem kell ellenorizni (az os megteszi).
            //

            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            if (!@interface.IsInstanceOfType(instance))
                throw new InvalidOperationException(string.Format(Resources.Culture, Resources.NOT_ASSIGNABLE, @interface, instance.GetType()));

            FService = releaseOnDispose ? new ServiceReference(this) : (AbstractServiceReference) new InstanceReference(this);
            FService.Instance = instance;
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

        public override void GetService(IInjector injector, ref AbstractServiceReference reference)
        {
            CheckDisposed();

            //
            // Az injector kontenereben is deklaralhatunk bejegyzeseket (pl az Injector felveszi sajat
            // magat) -> ne "injector.UnderlyingContainer.Parent.IsDescendantOf(Owner)" legyen.
            //

            Debug.Assert(injector.UnderlyingContainer.IsDescendantOf(Owner));

            reference.Release();
            reference = FService;
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged) FService.Release();

            base.Dispose(disposeManaged);
        }
    }
}