/********************************************************************************
* LazyType.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Threading;

namespace Solti.Utils.DI.Internals
{
    internal sealed class LazyType: Lazy<Type>
    {
        private readonly int FTypeId;

        private LazyType(Func<Type> factory, int typeId) : base(factory, LazyThreadSafetyMode.ExecutionAndPublication)
            => FTypeId = typeId;

        public static LazyType Create(Type iface, ITypeResolver resolver)
        {
            return new LazyType
            (
                Resolve,
#if NETSTANDARD2_0
                new { iface, resolver }.GetHashCode()
#else
                HashCode.Combine(iface, resolver)
#endif
            );
   
            Type Resolve()
            {
                Type implementation = resolver.Resolve(iface);

                Ensure.IsNotNull(implementation, nameof(implementation));
                Ensure.Type.Supports(implementation, iface);

                return implementation;
            }
        }

        public override int GetHashCode() => FTypeId;

        public override bool Equals(object obj) => (obj is LazyType type) && type.FTypeId == FTypeId;
    }
}
