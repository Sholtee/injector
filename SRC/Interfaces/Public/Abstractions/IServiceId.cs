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
    /// <remarks>A service can be identified uniquely by its <see cref="Interface"/> and <see cref="Name"/></remarks>
    public interface IServiceId
    {
        /// <summary>
        /// Related comparer.
        /// </summary>
        public sealed class Comparer : ComparerBase<Comparer, IServiceId>
        {
            /// <inheritdoc/>
            public override bool Equals(IServiceId x, IServiceId y) => x.Interface == y.Interface && x.Name == y.Name;

            /// <inheritdoc/>
            public override int GetHashCode(IServiceId obj) => unchecked(obj.Interface.GetHashCode() ^ (obj.Name?.GetHashCode() ?? 0));
        }

        /// <summary>
        /// The service interface.
        /// </summary>
        Type Interface { get; }

        /// <summary>
        /// The (optional) service name.
        /// </summary>
        object? Name { get; }
    }
}