/********************************************************************************
* Visibility.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Immutable;
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

            ImmutableArray<Diagnostic> diagnostics = compilation.GetDeclarationDiagnostics();

            return !diagnostics.Any(diag => diag.Severity == DiagnosticSeverity.Error);
        }

        public static bool GrantedFor(Type type, string asmName)
        {
            //
            // Mivel az "internal" es "protected" kulcsszavak nem leteznek IL szinten ezert itt reflexioval
            // nem tudjuk megallapitani h a tipus lathato e a kodunk szamara =(
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
            // [assembly: AssemblyTitle(nameof(member))]
            //

            CompilationUnitSyntax unitToCheck = CompilationUnit().WithAttributeLists
            (
                attributeLists: SingletonList
                (
                    AttributeList
                    (
                        attributes: SingletonSeparatedList
                        (
                            Attribute
                            (
                                name: (NameSyntax) CreateType<AssemblyTitleAttribute>()
                            )
                            .WithArgumentList
                            (
                                argumentList: AttributeArgumentList
                                (
                                    arguments: SingletonSeparatedList
                                    (
                                        AttributeArgument
                                        (
                                            expression: InvocationExpression(IdentifierName("nameof")).WithArgumentList
                                            (
                                                argumentList: ArgumentList
                                                (
                                                    SingletonSeparatedList
                                                    (
                                                        Argument
                                                        (
                                                            expression: MemberAccessExpression
                                                            (
                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                CreateType(member.DeclaringType),
                                                                IdentifierName(member.Name)
                                                            )
                                                        )
                                                    )
                                                )
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    )
                    .WithTarget(AttributeTargetSpecifier(Token(SyntaxKind.AssemblyKeyword)))
                )
            );

            return GrantedFor(unitToCheck, asmName, member.DeclaringType.Assembly());
        }
    }
}