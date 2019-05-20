/********************************************************************************
* ServiceAlreadyRegisteredException.cs                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI
{
    using Properties;

    public sealed class ServiceAlreadyRegisteredException: ArgumentException
    {
        public ServiceAlreadyRegisteredException(Type iface, Exception innerException = null): base(string.Format(Resources.ALREADY_REGISTERED, iface), innerException)
        {
        }
    }
}
