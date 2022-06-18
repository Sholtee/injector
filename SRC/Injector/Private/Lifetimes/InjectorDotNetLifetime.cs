/********************************************************************************
* InjectorDotNetLifetime.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal abstract class InjectorDotNetLifetime : Lifetime, IHasPrecedence
    {
        private static void Initialize<TLifetime>() where TLifetime: InjectorDotNetLifetime, new() => _ = new TLifetime();

        protected InjectorDotNetLifetime(int precedence) => Precedence = precedence;

        #pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
        [ModuleInitializer]
        #pragma warning restore CA2255
        public static void Initialize()
        {
            Initialize<InstanceLifetime>();
            Initialize<PooledLifetime>();
            Initialize<TransientLifetime>();
            Initialize<ScopedLifetime>();
            Initialize<SingletonLifetime>();
        }

        public int Precedence { get; }

        public override int CompareTo(Lifetime other) => other is IHasPrecedence hasPrecedence
            ? Precedence - hasPrecedence.Precedence
            : other.CompareTo(this) * -1;
    }
}
