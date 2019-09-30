/********************************************************************************
* ExcludeFromCoverageAttribute.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property)]
    [ExcludeFromCoverage]
    internal sealed class ExcludeFromCoverageAttribute: Attribute
    {
    }
}
