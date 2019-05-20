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
                ParameterInfo parameter = infos[i];

                ParameterIsAttribute attr = parameter.GetCustomAttribute<ParameterIsAttribute>();
                if (attr != null)
                {
                    object arg = args[i];

                    foreach (IValidator validator in attr.Validators)
                    {
                        validator.Validate(arg, parameter.Name);           
                    }
                }
            }

            return base.Invoke(targetMethod, args);
        }
    }
}
