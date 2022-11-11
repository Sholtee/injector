/********************************************************************************
* ServiceResolver.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Interfaces
{ 
    /// <summary>
    /// Creates the concrete service instance using the given factory.
    /// </summary>
    public delegate object ServiceResolver(IInstanceFactory instanceFactory);
}
