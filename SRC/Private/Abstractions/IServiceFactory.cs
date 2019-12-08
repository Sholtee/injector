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

        void GetService(Func<IInjector> injectorFactory, ref ServiceReference reference);
    }
}