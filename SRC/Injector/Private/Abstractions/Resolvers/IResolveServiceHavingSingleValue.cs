/********************************************************************************
* IResolveServiceHavingSingleValue.cs                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    internal interface IResolveServiceHavingSingleValue<TDescendant> : IInstanceFactory<TDescendant, object?> where TDescendant: IResolveServiceHavingSingleValue<TDescendant>
    {
    }
}
