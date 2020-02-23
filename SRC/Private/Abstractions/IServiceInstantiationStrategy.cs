/********************************************************************************
* IServiceInstantiationStrategy.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI.Internals
{
    internal interface IServiceInstantiationStrategy
    {
        bool ShouldUse(IInjector injector, AbstractServiceEntry requested);
        ServiceReference Exec(IInjectorEx injector, ServiceReference requestor, AbstractServiceEntry requested);
    }
}
