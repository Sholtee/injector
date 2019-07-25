/********************************************************************************
* IServiceFactory.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// All stuffs required to produce/query a service instance.
    /// </summary>
    internal interface IServiceFactory
    {
        Func<IInjector, Type, object> Factory { get; }

        Lifetime? Lifetime { get; }

        object Value { get; set; }
    }
}