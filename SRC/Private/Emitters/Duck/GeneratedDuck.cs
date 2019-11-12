/********************************************************************************
* GeneratedDuck.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal sealed class GeneratedDuck<TInterface, TTarget>: TypeGenerator<GeneratedDuck<TInterface, TTarget>> where TInterface: class
    {
        public override string AssemblyName => DuckGenerator<TTarget, TInterface>.AssemblyName;

        protected override Type GenerateType()
        {
            CheckInterface();
            CheckTarget();

            return GenerateType
            (
                @class: DuckGenerator<TTarget, TInterface>.GenerateDuckClass(),
                references: new[]
                {
                    typeof(DuckBase<>).Assembly()
                }
                .Concat(typeof(TInterface).GetReferences())
                .Concat(typeof(TTarget).GetReferences())
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

        private void CheckTarget()
        {
            //
            // Konstruktor parameterben atadasra kerul -> lathatonak kell lennie.
            //

            CheckVisibility(typeof(TTarget));
        }
    }
}