/********************************************************************************
* IPool.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;
    using Primitives.Threading;

    internal sealed record PoolScope<TInterface>(IInjector Scope, TInterface ServiceInstance) where TInterface: class;

    internal interface IPool<TInterface>: IDisposableEx where TInterface: class
    {
        IPoolItem<PoolScope<TInterface>> Get();
    }
}
