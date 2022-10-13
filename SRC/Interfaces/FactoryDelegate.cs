/********************************************************************************
* FactoryDelegate.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Represents a delegate that is reponsible for building a particular service.
    /// </summary>
    /// <returns></returns>
    [SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
    public delegate object FactoryDelegate(IInjector scope, out object? disposable);
}
