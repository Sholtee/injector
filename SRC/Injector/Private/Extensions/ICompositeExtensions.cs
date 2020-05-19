/********************************************************************************
* ICompositeExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Primitives.Patterns;

    internal static class ICompositeExtensions
    {
        internal static bool IsDescendantOf<T>(this IComposite<T> src, IComposite<T> parent) where T : class, IComposite<T>
        {
            for (IComposite<T>? current = src; current != null; current = current.Parent)
                if (current == parent) return true;
            return false;
        } 
    }
}
