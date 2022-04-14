/********************************************************************************
* IResolveService.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    internal interface IResolveService<TDescendant> : IInstanceFactory<TDescendant> where TDescendant: IResolveService<TDescendant>
    {
    }
}
