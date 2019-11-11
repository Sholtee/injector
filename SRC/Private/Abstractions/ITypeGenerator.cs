/********************************************************************************
* ITypeGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    internal interface ITypeGenerator
    {
        Type Type { get; }
    }
}
