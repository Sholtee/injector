﻿/********************************************************************************
* IPool.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Primitives.Patterns;

    internal interface IPool<TInterface>: IDisposableEx where TInterface: class
    {
        IPoolItem<TInterface> Get();
    }
}
