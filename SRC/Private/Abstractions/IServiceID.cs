/********************************************************************************
* IServiceID.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    internal interface IServiceID
    {
        Type Interface { get; }
        string Name { get; }
    }
}