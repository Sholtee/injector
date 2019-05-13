/********************************************************************************
* DependencyType.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI
{
    public enum DependencyType
    {
        Transient = 0,
        Singleton,
        InstantiatedSingleton, // internal
        Self // internal
    }
}