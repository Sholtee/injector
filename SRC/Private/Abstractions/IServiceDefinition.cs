/********************************************************************************
* IServiceDefinition.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    internal interface IServiceDefinition: IServiceId
    {
        IServiceContainer Owner { get; }
        Lifetime? Lifetime { get; }
        Type Implementation { get; }
        Func<IInjector, Type, object> Factory { get; }
    }
}
