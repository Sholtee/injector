/********************************************************************************
* IStatefulInjector.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    internal interface IStatefulInjector: IInjector
    {
        IReadOnlyDictionary<string, object> FactoryOptions { get; }
        ServiceGraph Graph { get; }
        void Instantiate(ServiceReference reference);
    }
}
