/********************************************************************************
* GeneratedDisposable.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal sealed class GeneratedDisposable<TInterface>: TypeGenerator<GeneratedDisposable<TInterface>> where TInterface: class
    {
        public override string AssemblyName => $"{ProxyGeneratorBase.CreateType<TInterface>()}_Disposable";

        protected override Type GenerateType()
        {
            CheckInterface();

            return GenerateType
            (
                @class: DisposableGenerator<TInterface>.GenerateProxyClass(),
                references: new[]
                {
                    typeof(DisposableWrapper<>).Assembly()
                }
                .Concat(typeof(TInterface).GetReferences())
                .Distinct()
            );
        }

        private void CheckInterface()
        {
            Type type = typeof(TInterface);

            CheckVisibility(type);

            if (!type.IsInterface()) throw new InvalidOperationException(Resources.NOT_AN_INTERFACE);
            if (type.ContainsGenericParameters()) throw new NotSupportedException();
        }
    }
}