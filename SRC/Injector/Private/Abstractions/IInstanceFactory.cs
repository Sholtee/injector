/********************************************************************************
* IInstanceFactory.cs                                                           *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal interface IInstanceFactory
    {
        object CreateInstance(AbstractServiceEntry requested);

        object GetOrCreateInstance(AbstractServiceEntry requested, int slot);
    }
}
