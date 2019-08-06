/********************************************************************************
* InjectornEventArg.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

using System;

namespace Solti.Utils.DI
{
    public class InjectorEventArg
    {
        public InjectorEventArg(Type target)
        {
            Target = target;
        }

        /// <summary>
        /// The (optional) target who invocated the <see cref="IInjector"/>.
        /// </summary>
        public Type Target { get; }

        /// <summary>
        /// The newly created service (if the invocation was successfull). 
        /// </summary>
        public object Service { get; set; }
    }
}
