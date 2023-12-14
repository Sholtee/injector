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
            public override bool Equals(IServiceId x, IServiceId y)
            {
                if (x is null)
                    throw new ArgumentNullException(nameof(x));

                if (y is null)
                    throw new ArgumentNullException(nameof(y));

                return
                    x.Type == y.Type &&
                    x.Key is null == y.Key is null &&

                    //
                    // When comparing keys use Equals() instead of reference check (for proper string comparison)
                    //

                    x.Key?.Equals(y.Key) is not false;
            }

            /// <inheritdoc/>
            public override int GetHashCode(IServiceId obj)
            {
                if (obj is null)
                    throw new ArgumentNullException(nameof(obj));

                return unchecked(obj.Type.GetHashCode() ^ (obj.Key?.GetHashCode() ?? 0));
            }
        }

        /// <summary>
        /// Related formatter
        /// </summary>
        public static class Formatter
        {
            /// <summary>
            /// Pretty prints the given <paramref name="serviceId"/>.
            /// </summary>
            public static string Format(IServiceId serviceId)
            {
                if (serviceId is null)
                    throw new ArgumentNullException(nameof(serviceId));

                string result = serviceId.Type.GetFriendlyName();
                if (serviceId.Key is not null)
                    result += $":{serviceId.Key}";
                return result;
            }
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