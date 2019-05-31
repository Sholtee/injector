/********************************************************************************
* MethodValidatorProxy.cs                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal sealed class MethodValidatorProxy<TInterface> : InterfaceProxy<TInterface> where TInterface: IStateful
    {
        public MethodValidatorProxy(TInterface target) : base(target)
        {
        }

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            if (Target.Locked && targetMethod.GetCustomAttribute<StateCriticalAttribute>() != null)
                throw new InvalidOperationException(Resources.LOCKED);

            return base.Invoke(targetMethod, args);
        }
    }
}
