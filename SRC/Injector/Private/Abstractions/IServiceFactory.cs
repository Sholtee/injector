/********************************************************************************
* IServiceFactory.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal interface IServiceFactory
    {
        object GetOrCreateInstance(AbstractServiceEntry requested);
    }
}
