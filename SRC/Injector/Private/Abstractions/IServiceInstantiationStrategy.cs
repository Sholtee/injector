/********************************************************************************
* IServiceInstantiationStrategy.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal interface IServiceInstantiationStrategy
    {
        bool ShouldUse(Injector injector, AbstractServiceEntry requested);
        IServiceReference Exec(Injector injector, IServiceReference? requestor, AbstractServiceEntry requested);
    }
}
