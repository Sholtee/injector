/********************************************************************************
* IPoolItem.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Primitives.Patterns;

    internal interface IPoolItem<TInterface> : IDisposableEx where TInterface : class
    {
        TInterface Value { get; }
    }
}
