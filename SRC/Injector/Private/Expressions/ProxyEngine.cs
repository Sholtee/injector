/********************************************************************************
* ProxyEngine.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq.Expressions;

namespace Solti.Utils.DI.Internals
{
    internal sealed partial class ProxyEngine
    {
        public Expression CreateActivatorExpression(Type proxy, Expression injector, Expression target, Expression interceptorArray) => Expression.New
        (
            proxy.GetApplicableConstructor(),
            target,
            interceptorArray
        );
    }
}
