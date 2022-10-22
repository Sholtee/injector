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
    internal interface ILookup<TData>: IReadOnlyLookup<TData>
    {
        /// <summary>
        /// Tries to expand the current lookup.
        /// </summary>
        bool TryAdd(CompositeKey key, TData data);
    }
}
