/********************************************************************************
* IResolveGenericService.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal interface IResolveGenericService<TDescendant> : IInstanceFactory<TDescendant, Node<Type, AbstractServiceEntry>?> where TDescendant : IResolveGenericService<TDescendant>
    {
    }
}
