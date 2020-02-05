/********************************************************************************
* InstanceReference.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    internal sealed class InstanceReference : AbstractServiceReference
    {
        public InstanceReference(AbstractServiceEntry entry) : base(entry) { }

        public override ICollection<AbstractServiceReference> Dependencies => Array.Empty<AbstractServiceReference>();

        protected override void Dispose(bool disposeManaged)
        {
            Debug.WriteLine($"Disposed instance: {RelatedServiceEntry.Interface.Name}");
            base.Dispose(disposeManaged);
        }
    }
}
