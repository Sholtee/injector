/********************************************************************************
* GeneratedDuck.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    using static ProxyGeneratorBase;
    using static Compile;

    internal sealed class GeneratedDuck<TInterface, TTarget>: ITypeGenerator where TInterface: class
    {
        public Type Type
        {
            get
            {
                if (FType == null)
                    lock (FLock)
                        if (FType == null) FType = GenerateType();
                return FType;
            }
        }

        public string AssemblyName => DuckGenerator<TTarget, TInterface>.AssemblyName;

        #region Private
        private static readonly object FLock = new object();

        private static Type FType;

        private Type GenerateType()
        {
            CheckInterface();
            CheckTarget();

            Assembly[] references = new[]
            {
                typeof(DuckBase<>).Assembly()
            }
            .Concat(typeof(TInterface).GetReferences())
            .Concat(typeof(TTarget).GetReferences())
            .Distinct()
            .ToArray();

            return ToAssembly
            (
                root: GenerateProxyUnit
                (
                    @class: DuckGenerator<TTarget, TInterface>.GenerateDuckClass()
                ), 
                asmName: AssemblyName, 
                references: references
            )
            .GetType(GeneratedClassName, throwOnError: true);
        }

        private void CheckInterface()
        {
            Type type = typeof(TInterface);

            CheckVisibility(type, AssemblyName);

            if (!type.IsInterface()) throw new InvalidOperationException(Resources.NOT_AN_INTERFACE);
            if (type.ContainsGenericParameters()) throw new NotSupportedException();
        }

        private void CheckTarget()
        {
            //
            // Konstruktor parameterben atadasra kerul -> lathatonak kell lennie.
            //

            CheckVisibility(typeof(TTarget), AssemblyName);
        }
        #endregion
    }
}