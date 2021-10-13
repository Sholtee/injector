/********************************************************************************
* IgnoreAttribute.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Filters;

namespace Solti.Utils.DI.Perf
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class IgnoreAttribute: FilterConfigBaseAttribute
    {
        public IgnoreAttribute() : base(new SimpleFilter(_ => false)) { }
    }
}
