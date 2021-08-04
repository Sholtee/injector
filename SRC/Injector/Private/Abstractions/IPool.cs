﻿/********************************************************************************
* IPool.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal interface IPool
    {
        IWrapped Get();
    }

    internal interface IPool<TInterface>: IPool where TInterface: class
    {
    }
}
