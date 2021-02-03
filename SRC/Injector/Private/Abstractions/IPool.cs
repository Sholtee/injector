/********************************************************************************
* IPool.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Primitives.Patterns;

    internal interface IPool
    {
        object Get(CheckoutPolicy checkoutPolicy);
    }

    internal interface IPool<TInterface> where TInterface: class
    {
        PoolItem<TInterface> Get(CheckoutPolicy checkoutPolicy);
    }
}
