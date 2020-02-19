/********************************************************************************
* IServiceInstantiationStrategy.cs                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI.Internals
{
    internal interface IServiceInstantiationStrategy
    {
        bool ShouldUse(Injector injector, AbstractServiceEntry requested);
        ServiceReference Exec(Injector injector, ServiceReference requestor, AbstractServiceEntry requested);
    }
}
