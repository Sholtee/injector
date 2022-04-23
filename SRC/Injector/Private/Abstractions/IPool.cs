/********************************************************************************
* IPool.cs                                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    internal interface IPool
    {
        object Get();

        void Return(object instance);
    }

    internal interface IPool<TInterface>: IPool where TInterface: class
    {
    }
}
