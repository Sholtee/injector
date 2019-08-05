/********************************************************************************
* IServiceFactory.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    internal interface IServiceFactory
    {
        Func<IInjector, Type, object> Factory { get; set; }

        object GetService(IInjector injector, Type iface = null);
    }
}