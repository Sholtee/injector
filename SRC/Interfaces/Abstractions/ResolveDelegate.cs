/********************************************************************************
* ResolveDelegate.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Gets or creates a particular service.
    /// </summary>
    public delegate object ResolveDelegate(IInstanceFactory);
}
