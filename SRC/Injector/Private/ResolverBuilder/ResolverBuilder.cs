/********************************************************************************
* ResolverBuilder.cs                                                            *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    /// <summary>
    /// 
    /// </summary>
    public delegate AbstractServiceEntry? Resolver(IServiceRegistry self, Type iface, string? name);

    internal abstract partial class ResolverBuilder
    {
        public delegate Resolver ResolverCaseBuilder(int slot, AbstractServiceEntry entry);

        public abstract Resolver Build(IEnumerable<AbstractServiceEntry> entries, ResolverCaseBuilder regularEntryResolverBuilder, ResolverCaseBuilder genericEntryResolverBuilder, out int reCount, out int geCount, CancellationToken cancellation = default);

        public static ResolverBuilder ChainedDelegates { get; } = new ChainedDelegatesResolverBuilder();

        public static ResolverBuilder CompiledExpression { get; } = new CompiledExpressionResolverBuilder();

        public static ResolverBuilder CompiledCode { get; } = new CompiledCodeResolverBuilder();
    }
}
