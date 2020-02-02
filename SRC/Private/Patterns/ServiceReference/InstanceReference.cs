/********************************************************************************
* InstanceReference.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Encapsulates a service and its dependencies into a reference counted container.
    /// </summary>
    public class InstanceReference : AbstractServiceReference
    {
        /// <summary>
        /// Creates a new <see cref="InstanceReference"/> instance.
        /// </summary>
        public InstanceReference(AbstractServiceEntry entry) : base(entry) { }

        /// <summary>
        /// See <see cref="AbstractServiceReference.Dependencies"/>.
        /// </summary>
        public override ICollection<AbstractServiceReference> Dependencies => Array.Empty<AbstractServiceReference>();
    }
}
