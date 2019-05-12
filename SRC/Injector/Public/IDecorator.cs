/********************************************************************************
* IDecorator.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using JetBrains.Annotations;

namespace Solti.Utils.Injector
{
    public interface IDecorator
    {
        IDecorator Decorate([NotNull] Func<Type, object, object> decorator);
    }

    public interface IDecorator<TInterface>: IDecorator
    {
        IDecorator<TInterface> Decorate([NotNull] Func<TInterface, TInterface> decorator);
    }
}