/********************************************************************************
* ParameterValidator.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Reflection;

using JetBrains.Annotations;

namespace Solti.Utils.DI
{
    internal sealed class ParameterValidator<TInterface> : InterfaceProxy<TInterface>
    {
        public ParameterValidator(TInterface target): base(target)
        {
        }

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            ParameterInfo nullPara = targetMethod
                .GetParameters()
                .Where((para, i) => para.GetCustomAttribute<NotNullAttribute>() != null && args[i] == null)
                .FirstOrDefault();

            if (nullPara != null) throw new ArgumentNullException(nullPara.Name);

            return base.Invoke(targetMethod, args);
        }
    }
}
