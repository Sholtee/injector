/********************************************************************************
* IInjectorEx.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    internal interface IInjectorEx: IInjector
    {
        IInjectorEx Spawn(IServiceContainer parent);
        void Instantiate(ServiceReference requested);
    }
}
