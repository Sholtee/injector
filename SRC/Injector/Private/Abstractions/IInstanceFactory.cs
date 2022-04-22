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
        IInstanceFactory? Super { get; }

        object CreateInstance(AbstractServiceEntry requested);

        object GetOrCreateInstance(AbstractServiceEntry requested, int slot);
    }
}
