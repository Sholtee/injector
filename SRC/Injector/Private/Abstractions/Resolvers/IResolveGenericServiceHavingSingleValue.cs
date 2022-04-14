/********************************************************************************
* IResolveGenericServiceHavingSingleValue.cs                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    internal interface IResolveGenericServiceHavingSingleValue<TDescendant> : IInstanceFactory<TDescendant, Node<Type, object>?> where TDescendant : IResolveGenericServiceHavingSingleValue<TDescendant>
    {
    }
}
