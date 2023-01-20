/********************************************************************************
* IPool.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    internal interface IPool: IDisposable
    {
        object Get();

        void Return(object instance);
    }

    //
    // Required to handle generic services
    //

    internal interface IPool<TInterface>: IPool where TInterface: class
    {
    }
}
