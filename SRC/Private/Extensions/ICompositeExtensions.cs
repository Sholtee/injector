/********************************************************************************
* ICompositeExtensions.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    internal static class ICompositeExtensions
    {
        internal static bool IsDescendantOf<T>(this IComposite<T> src, IComposite<T> parent) where T : IComposite<T>
        {
            for (; src != null; src = src.Parent)
                if (src == parent) return true;
            return false;
        } 
    }
}
