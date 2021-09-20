/********************************************************************************
* ServiceId.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed record ServiceId(Type Interface, string? Name) : IServiceId
    {
    }
}
