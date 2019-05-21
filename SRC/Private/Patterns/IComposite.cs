/********************************************************************************
* IComposite.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI
{
    public interface IComposite<T>: IDisposable where T : IComposite<T>
    {
        T Parent { get; }
        IReadOnlyList<T> Children { get; }
        T CreateChild();
    }
}