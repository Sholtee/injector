/********************************************************************************
* CompilationOptionsFactory.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Solti.Utils.DI.Internals
{
    internal static class CompilationOptionsFactory
    {
        private static readonly PropertyInfo TopLevelBinderFlagsProperty = typeof(CSharpCompilationOptions)
            .GetTypeInfo()
            .GetProperty("TopLevelBinderFlags", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly uint IgnoreAccessibility;

        static CompilationOptionsFactory()
        {
            Type binderFlagsType = typeof(CSharpCompilationOptions)
                .GetTypeInfo()
                .Assembly
                .GetType("Microsoft.CodeAnalysis.CSharp.BinderFlags");
            FieldInfo
            /*
                ignoreCorLibraryDuplicatedTypesMember = binderFlagsType
                    .GetTypeInfo()
                    .GetField("IgnoreCorLibraryDuplicatedTypes", BindingFlags.Static | BindingFlags.Public),
            */
                ignoreAccessibility = binderFlagsType
                    .GetTypeInfo()
                    .GetField("IgnoreAccessibility", BindingFlags.Static | BindingFlags.Public);

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
