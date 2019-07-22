/********************************************************************************
* InterfaceInterceptor.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    public abstract class InterfaceInterceptor
    {
        protected abstract object Invoke(MethodInfo method, object[] args);
    }
}
