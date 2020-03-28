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
            //
            // "HashCode.Combine()" csak netstandard2.1-tol van ezert az anonim objektum.
            //

            return new LazyType(Resolve, new { iface, resolver }.GetHashCode());
   
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
