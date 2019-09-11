/********************************************************************************
* CompilationOptionsFactory.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Solti.Utils.DI.Internals
{
    internal static class CompilationOptionsFactory
    {
        private static readonly PropertyInfo TopLevelBinderFlagsProperty;

        private static readonly uint IgnoreAccessibility;

        static CompilationOptionsFactory()
        {
            TopLevelBinderFlagsProperty = typeof(CSharpCompilationOptions)
                .GetProperty("TopLevelBinderFlags", BindingFlags.Instance | BindingFlags.NonPublic);
            Debug.Assert(TopLevelBinderFlagsProperty != null);

            Type binderFlagsType = typeof(CSharpCompilationOptions)
                .Assembly()
                .GetType("Microsoft.CodeAnalysis.CSharp.BinderFlags");
            Debug.Assert(binderFlagsType != null);

            FieldInfo ignoreAccessibility = binderFlagsType
                .GetField("IgnoreAccessibility", BindingFlags.Static | BindingFlags.Public);
            Debug.Assert(ignoreAccessibility != null);

            IgnoreAccessibility = (uint) ignoreAccessibility.GetValue(null);
        }

        public static CSharpCompilationOptions Create(bool ignoreAccessChecks)
        {
            var options = new CSharpCompilationOptions
            (
                outputKind: OutputKind.DynamicallyLinkedLibrary,
                metadataImportOptions: ignoreAccessChecks ? MetadataImportOptions.All : MetadataImportOptions.Public,
                optimizationLevel: OptimizationLevel.Release
            );

            if (ignoreAccessChecks) TopLevelBinderFlagsProperty.FastSetValue(options, IgnoreAccessibility);

            return options;
        }
    }
}
