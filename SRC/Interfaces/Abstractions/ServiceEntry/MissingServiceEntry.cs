/********************************************************************************
* MissingServiceEntry.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Describes a missing service.
    /// </summary>
    /// <remarks>This entry cannot be instantiated.</remarks>
    public sealed class MissingServiceEntry : AbstractServiceEntry
    {
        /// <summary>
        /// Creates a new <see cref="MissingServiceEntry"/> instance.
        /// </summary>
        public MissingServiceEntry(Type @interface, string? name) : base(@interface, name, null, null) { }

        /// <inheritdoc/>
        public override object Resolve(IInstanceFactory instanceFactory) => throw new NotImplementedException();
    }
}