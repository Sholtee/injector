/********************************************************************************
* IServiceGraph.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal interface IServiceGraph: IEnumerable<IServiceReference>
    {
    }
}
