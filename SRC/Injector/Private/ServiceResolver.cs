/********************************************************************************
* ServiceResolver.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed record ServiceResolver(AbstractServiceEntry RelatedEntry, Func<IInstanceFactory, object> Resolve);
}
