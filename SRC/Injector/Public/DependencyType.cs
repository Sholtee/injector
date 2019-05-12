/********************************************************************************
* DependencyType.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.Injector
{
    public enum DependencyType
    {
        Transient = 0,
        Singleton,
        InstantiatedSingleton, // internal
        Self // internal
    }
}