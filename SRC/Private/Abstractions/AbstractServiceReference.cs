/********************************************************************************
* AbstractServiceReference.cs                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Encapsulates a service and its dependencies into a reference counted container.
    /// </summary>
    public abstract class AbstractServiceReference : DisposeByRefObject
    {
        /// <summary>
        /// Creates a new <see cref="AbstractServiceReference"/> instance.
        /// </summary>
        public AbstractServiceReference(AbstractServiceEntry entry) => RelatedServiceEntry = entry;

        /// <summary>
        /// The related service entry.
        /// </summary>
        public AbstractServiceEntry RelatedServiceEntry { get; }

        /// <summary>
        /// The referenced service instance.
        /// </summary>
        public object Instance { get; set; }

        /// <summary>
        /// The dependencies of this service <see cref="Instance"/>.
        /// </summary>
        public abstract ICollection<AbstractServiceReference> Dependencies { get; }
    }
}
