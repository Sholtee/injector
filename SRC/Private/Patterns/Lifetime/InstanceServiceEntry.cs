/********************************************************************************
* InstanceServiceEntry.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes a singleton service entry.
    /// </summary>
    public class InstanceServiceEntry : ServiceEntry
    {
        private readonly bool FReleaseOnDispose;

        public InstanceServiceEntry(Type @interface, object value, bool releaseOnDispose) : base(@interface, null)
        {
            Value = value;
            FReleaseOnDispose = releaseOnDispose;
        }

        public override Type Implementation => null;

        public override Func<IInjector, Type, object> Factory
        {
            get => throw new InvalidOperationException();
            set => throw new InvalidOperationException();
        }
        public override object Value { get; }
        public override object GetService(IInjector injector) => Value;

        public override object Clone() => new InstanceServiceEntry(Interface, Value, releaseOnDispose: false /*csak egyszer szabaditsuk fel*/);

        public override bool IsService => false;
        public override bool IsLazy => false;
        public override bool IsFactory => false;
        public override bool IsInstance => true;

        protected override void Dispose(bool disposeManaged)
        {
            if (FReleaseOnDispose) (Value as IDisposable)?.Dispose();
            base.Dispose(disposeManaged);
        }
    }
}