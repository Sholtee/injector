/********************************************************************************
* DelegateExtensions.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    internal static class DelegateExtensions
    {
//      public static Func<T, bool> And<T>(this Func<T, bool> predicate, Func<T, bool> extension) => x => predicate(x) && extension(x);

        public static Func<T, bool> Or<T>(this Func<T, bool> predicate, Func<T, bool> extension) => x => predicate(x) || extension(x);
    }
}
