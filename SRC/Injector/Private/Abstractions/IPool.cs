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
        IWrapped<object> Get();
    }

    internal interface IPool<TInterface>: IPool where TInterface: class
    {
    }
}
