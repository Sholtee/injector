/********************************************************************************
* Visibility.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.DI.Internals
{
    using static ProxyGeneratorBase;

    internal static class Visibility
    {
        private static bool GrantedFor(CompilationUnitSyntax unitToCheck, string asmName, params Assembly[] references)
        {
            //
            // Mivel az "internal" es "protected" kulcsszavak nem leteznek IL szinten ezert reflexioval
            // nem tudnank megallapitani h a tipus lathato e a kodunk szamara szoval a forditora bizzuk
            // a dontest.
            //

            Debug.WriteLine(unitToCheck.NormalizeWhitespace().ToFullString());

            CSharpCompilation compilation = CSharpCompilation.Create
            (
                assemblyName: asmName,
                syntaxTrees: new[]
                {
                    CSharpSyntaxTree.Create
                    (
                        root: unitToCheck
                    )
                },
                references: Runtime
                    .Assemblies
                    .Concat(references)
                    .Distinct()
                    .Select(asm => MetadataReference.CreateFromFile(asm.Location)),
                options: CompilationOptionsFactory.Create()
            );

            Diagnostic[] diagnostics = compilation
                .GetDeclarationDiagnostics()
                .Where(diag => diag.Severity == DiagnosticSeverity.Error)
                .ToArray();

            Debug.Assert(diagnostics.Length <= 1, "Too many errors");

            //
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs0122
            //

            return !diagnostics.Any(diag => diag.Id.Equals("CS0122", StringComparison.OrdinalIgnoreCase));
        }

        public static bool GrantedFor(Type type, string asmName)
        {
            //
            // using t = Namespace.Type;
            //

            CompilationUnitSyntax unitToCheck = CompilationUnit().WithUsings
            (
                usings: SingletonList
                (
                    UsingDirective
                    (
                        name: (NameSyntax) CreateType(type)
                    )
                    .WithAlias
                    (
                        alias: NameEquals(IdentifierName("t"))
                    )
                )
            );

            return GrantedFor(unitToCheck, asmName, type.GetReferences().ToArray());
        }

        public static bool GrantedFor(MemberInfo member, string asmName) 
        {
            //
            // public class Cls 
            // {
            //    private void Foo() {}
            //    public void Foo(int i) {}
            // }
            //
            // "nameof(Cls.Foo)" fordulni fog meg ha mi a parameter nelkuli metodust is vizsgalnank.
            //
            // Megjegyzendo h "Foo"-val mar nem lehet property szoval azzal nem lenne gond.
            //

            throw new NotImplementedException();
        }
    }
}