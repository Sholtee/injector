/********************************************************************************
* IServiceId.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Interfaces
{
    using Primitives;

    /// <summary>
    /// Describes an abstract service identifier.
    /// </summary>
    /// <remarks>A service can be identified uniquely by its <see cref="Type"/> and <see cref="Key"/></remarks>
    public interface IServiceId
    {
        /// <summary>
        /// Related comparer.
        /// </summary>
        public sealed class Comparer : ComparerBase<Comparer, IServiceId>
        {
            /// <inheritdoc/>
            public override bool Equals(IServiceId x, IServiceId y) => x.Type == y.Type && x.Key == y.Key;

            /// <inheritdoc/>
            public override int GetHashCode(IServiceId obj) => unchecked(obj.Type.GetHashCode() ^ (obj.Key?.GetHashCode() ?? 0));
        }

        /// <summary>
        /// Type of the service.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// The (optional) service key (usually a name).
        /// </summary>
        object? Key { get; }
    }
}