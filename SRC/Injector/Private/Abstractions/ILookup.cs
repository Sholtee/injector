/********************************************************************************
* ILookup.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes a generic lookup.
    /// </summary>
    internal interface ILookup<TKey, TData, TImplementation> where TImplementation: ILookup<TKey, TData, TImplementation>
    {
        /// <summary>
        /// Tries to expand the current lookup.
        /// </summary>
        bool TryAdd(TKey key, TData data);

        /// <summary>
        /// Adds a new entry, returns the expanded copy of this lookup.
        /// </summary>
        TImplementation With(TKey key, TData data);

        /// <summary>
        /// Tries to get the <paramref name="data"/> associated with the given <paramref name="key"/>.
        /// </summary>
        bool TryGet(TKey key, out TData data);

        /// <summary>
        /// The number of held items.
        /// </summary>
        int Count { get; }
    }
}
