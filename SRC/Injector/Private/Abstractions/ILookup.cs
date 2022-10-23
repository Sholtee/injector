/********************************************************************************
* ILookup.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes a generic lookup where keys are <see cref="CompositeKey"/>s.
    /// </summary>
    internal interface ILookup<TData, TImplementation> where TImplementation: ILookup<TData, TImplementation>
    {
        /// <summary>
        /// Tries to expand the current lookup.
        /// </summary>
        bool TryAdd(CompositeKey key, TData data);

        /// <summary>
        /// Adds a new entry, returns the expanded copy of this lookup.
        /// </summary>
        TImplementation Add(CompositeKey key, TData data);

        /// <summary>
        /// Tries to get the <paramref name="data"/> associated with the given <paramref name="key"/>.
        /// </summary>
        bool TryGet(CompositeKey key, out TData data);
    }
}
