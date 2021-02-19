/********************************************************************************
* IPool.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Threading;

    internal interface IPool: IEnumerable<(int OwnerThread, IServiceReference Object)>
    {
        PoolItem<IServiceReference>? Get(CheckoutPolicy checkoutPolicy);
    }

    internal interface IPool<TInterface>: IPool where TInterface: class
    {
    }
}
