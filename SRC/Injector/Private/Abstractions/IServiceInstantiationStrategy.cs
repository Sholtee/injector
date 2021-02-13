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
        bool ShouldUse(IInjector injector, AbstractServiceEntry requested);
        IServiceReference Exec(IInjector injector, AbstractServiceEntry requested);
    }
}
