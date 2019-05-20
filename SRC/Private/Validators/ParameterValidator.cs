/********************************************************************************
* ParameterValidator.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Reflection;

namespace Solti.Utils.DI
{
    internal sealed class ParameterValidator<TInterface> : InterfaceProxy<TInterface>
    {
        public ParameterValidator(TInterface target): base(target)
        {
        }

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            IReadOnlyList<ParameterInfo> infos = targetMethod.GetParameters();

            for (int i = 0; i < infos.Count; i++)
            {
                ParameterInfo info = infos[i];
                object arg = args[i];

                foreach (ExpectAttribute attr in info.GetCustomAttributes<ExpectAttribute>())
                {
                    attr.Validator.Validate(arg, info.Name);
                }
            }

            return base.Invoke(targetMethod, args);
        }
    }
}
