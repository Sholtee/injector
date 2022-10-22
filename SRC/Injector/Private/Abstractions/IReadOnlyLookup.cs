/********************************************************************************
* ILookup.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes a generic read-only lookup where keys are <see cref="CompositeKey"/>s.
    /// </summary>
    internal interface IReadOnlyLookup<TData>
    {
        /// <summary>
        /// Adds a new entry, returns the expanded copy of this lookup.
        /// </summary>
        IReadOnlyLookup<TData> Add(CompositeKey key, TData data);

        /// <summary>
        /// Tries to get the <paramref name="data"/> associated with the given <paramref name="key"/>.
        /// </summary>
        bool TryGet(CompositeKey key, out TData data);
    }
}
