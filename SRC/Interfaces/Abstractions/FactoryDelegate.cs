/********************************************************************************
* FactoryDelegate.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Unconditionaly creates a particular service.
    /// </summary>
    public delegate object FactoryDelegate(IInstanceFactory scope, out object? disposable);
}
