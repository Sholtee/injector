/********************************************************************************
* IPool.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;

    internal interface IPool
    {
        PoolItem<IServiceReference> Get(CheckoutPolicy checkoutPolicy);
    }

    internal interface IPool<TInterface>: IPool where TInterface: class
    {
    }
}
