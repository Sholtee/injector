/********************************************************************************
* Array.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    internal static class Array<T>
    {
        [SuppressMessage("Performance", "CA1825:Avoid zero-length array allocations")]
        public static readonly T[] Empty = new T[0];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] Create(int size) => size > 0
            ? new T[size]
            : Empty;
    }
}
