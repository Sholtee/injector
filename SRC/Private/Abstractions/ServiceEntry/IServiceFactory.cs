/********************************************************************************
* IServiceFactory.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    internal interface IServiceFactory
    {
        ServiceReference? Instance { get; }

        Func<IInjector, Type, object>? Factory { get; }

        bool SetInstance(ServiceReference reference, IReadOnlyDictionary<string, object> options);
    }
}