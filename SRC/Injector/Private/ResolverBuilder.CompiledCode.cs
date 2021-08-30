/********************************************************************************
* ResolverBuilder.CompiledCode.cs                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives.Patterns;

    internal abstract partial class ResolverBuilder
    {
        private sealed class CompiledCodeResolverBuilder : ResolverBuilder
        {
            private static TypeSyntax GetQualifiedName(Type type)
            {
                TypeSyntax ns = Qualify
                (
                    type
                        .FullName
                        .Replace
                        (
                            "[]",
                            string.Empty
#if NETSTANDARD2_1_OR_GREATER
                            , StringComparison.OrdinalIgnoreCase
#endif
                        )
                        .Split(Type.Delimiter)
                        .Select(IdentifierName)
                );

                if (type.IsArray) ns = ArrayType(elementType: ns).WithRankSpecifiers
                (
                    SingletonList
                    (
                        ArrayRankSpecifier
                        (
                            SingletonSeparatedList<ExpressionSyntax>(OmittedArraySizeExpression())
                        )
                    )
                );

                return ns;

                static NameSyntax Qualify(IEnumerable<NameSyntax> parts) => parts.Count() <= 1 ? parts.Single() : QualifiedName
                (
#if NETSTANDARD2_1_OR_GREATER
                    left: Qualify(parts.SkipLast(1)),         
#else
                    left: Qualify(parts.Take(parts.Count() - 1)),
#endif
                    right: (SimpleNameSyntax) parts.Last()
                );
            }

            private static SwitchStatementSyntax CreateSwitch(ExpressionSyntax value, IEnumerable<(string? Key, StatementSyntax[] Statements)> cases) => SwitchStatement(value).WithSections
            (
                List
                (
                    cases.Select
                    (
                        @case => SwitchSection()
                            .WithLabels
                            (
                                SingletonList<SwitchLabelSyntax>
                                (
                                    CaseSwitchLabel
                                    (
                                        @case.Key is not null
                                            ? LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(@case.Key))
                                            : LiteralExpression(SyntaxKind.NullLiteralExpression)
                                    )
                                )
                            )
                            .WithStatements
                            (
                                List(@case.Statements)
                            )
                    )
                )
            );

            private static SeparatedSyntaxList<TNode> CommaSeparatedList<TNode>(params TNode[] nodes) where TNode : SyntaxNode
            {
                return SeparatedList<TNode>
                (
                    nodesAndTokens: nodes.SelectMany(Node)
                );

                IEnumerable<SyntaxNodeOrToken> Node(TNode p, int i)
                {
                    yield return p;
                    if (i < nodes.Length - 1)
                        yield return Token(SyntaxKind.CommaToken);
                }
            }

            private const string MainMethod = "Create";

            private static MethodDeclarationSyntax CreateMainMethod(IEnumerable<AbstractServiceEntry> entries, Func<AbstractServiceEntry, int> createRegularResolver, Func<AbstractServiceEntry, int> createGenericResover)
            {
                ParameterSyntax
                    regularResolvers = Parameter(Identifier(nameof(regularResolvers))).WithType(GetQualifiedName(typeof(Resolver[]))),
                    genericResolvers = Parameter(Identifier(nameof(genericResolvers))).WithType(GetQualifiedName(typeof(Resolver[]))),

                    self  = Parameter(Identifier(nameof(self))),
                    iface = Parameter(Identifier(nameof(iface))),
                    name  = Parameter(Identifier(nameof(name)));

                LambdaExpressionSyntax lambda = ParenthesizedLambdaExpression()
                    .WithParameterList
                    (
                        ParameterList
                        (
                            CommaSeparatedList(self, iface, name)
                        )
                    )
                    .WithBlock
                    (
                        Block
                        (
                            CreateSwitch
                            (
                                value: InvocationExpression
                                (
                                    MemberAccessExpression
                                    (
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        MemberAccessExpression
                                        (
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName(iface.Identifier),
                                            IdentifierName(nameof(Type.GUID))
                                        ),
                                        IdentifierName(nameof(ToString))
                                    )
                                ),
                                cases: entries
                                    .GroupBy(entry => entry.Interface.GUID.ToString())
                                    .Select
                                    (
                                        grp => 
                                        (
                                            (string?) grp.Key,
                                            new StatementSyntax[]
                                            {
                                                 CreateSwitch
                                                 (
                                                     value: IdentifierName(name.Identifier),
                                                     cases: grp.Select
                                                     (
                                                        entry => 
                                                        (
                                                           entry.Name, 
                                                           new StatementSyntax[] { CreateResolveStatement(entry) }
                                                        )
                                                     )
                                                 ),
                                                 BreakStatement()
                                            }
                                        )
                                    )
                            ),
                            ReturnStatement
                            (
                                LiteralExpression(SyntaxKind.NullLiteralExpression)
                            )
                        )
                    );

                return MethodDeclaration
                (
                    GetQualifiedName(typeof(Resolver)),
                    Identifier(MainMethod)
                )
                .WithModifiers
                (
                    TokenList
                    (
                        new[]{ Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword) }
                    )
                )
                .WithParameterList
                (
                    ParameterList
                    (
                        CommaSeparatedList(regularResolvers, genericResolvers)   
                    )
                )
                .WithBody
                (
                    Block(SingletonList<StatementSyntax>(ReturnStatement(lambda)))
                );

                StatementSyntax CreateResolveStatement(AbstractServiceEntry entry)
                {
                    InvocationExpressionSyntax invokeResolver = InvocationExpression
                    (
                        ElementAccessExpression
                        (
                            IdentifierName((entry.Interface.IsGenericTypeDefinition ? genericResolvers : regularResolvers).Identifier)
                        )
                        .WithArgumentList
                        (
                            BracketedArgumentList
                            (
                                SingletonSeparatedList
                                (
                                    Argument
                                    (
                                        LiteralExpression
                                        (
                                            SyntaxKind.NumericLiteralExpression,
                                            Literal
                                            (
                                                (entry.Interface.IsGenericTypeDefinition ? createGenericResover : createRegularResolver).Invoke(entry)
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    )
                    .WithArgumentList
                    (
                        ArgumentList
                        (
                            CommaSeparatedList
                            (
                                Argument(IdentifierName(self.Identifier)),
                                Argument(IdentifierName(iface.Identifier)),
                                Argument(IdentifierName(name.Identifier))
                            )
                        )
                    );

                    if (!entry.IsShared)
                        return ReturnStatement(invokeResolver);

                    InvocationExpressionSyntax invokeParent = InvocationExpression
                    (
                        MemberAccessExpression
                        (
                            SyntaxKind.SimpleMemberAccessExpression,
                            MemberAccessExpression
                            (
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(self.Identifier),
                                IdentifierName(nameof(ServiceRegistryBase.Parent))
                            ),
                            IdentifierName(nameof(ServiceRegistryBase.GetEntry))
                        )
                    )
                    .WithArgumentList
                    (
                        ArgumentList
                        (
                            CommaSeparatedList
                            (
                                Argument(IdentifierName(iface.Identifier)),
                                Argument(IdentifierName(name.Identifier))
                            )
                        )
                    );

                    return ReturnStatement
                    (
                        ConditionalExpression
                        (
                            BinaryExpression
                            (
                                kind: SyntaxKind.NotEqualsExpression,
                                left: MemberAccessExpression
                                (
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName(self.Identifier),
                                    IdentifierName(nameof(ServiceRegistryBase.Parent))
                                ),
                                right: LiteralExpression(SyntaxKind.NullLiteralExpression)
                            ),
                            invokeParent,
                            invokeResolver
                        )
                    );
                }
            }

            private static IEnumerable<Assembly> GetRuntimeAssemblies()
            {
                yield return typeof(object).Assembly;
#if NETSTANDARD
                string[] mandatoryAssemblies =
                {
                    "System.Runtime",
                    "netstandard"
                };

                if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is not string asms)
                    yield break;

                foreach (string assemblyPath in asms.Split(Path.PathSeparator))
                {
                    string fileName = Path.GetFileNameWithoutExtension(assemblyPath);
                    if (mandatoryAssemblies.Contains(fileName))
                        yield return Assembly.LoadFile(assemblyPath);
                }
#endif
            }

            //
            // public static class ResolverFactory
            // {
            //     public static Resolver Create(Resolver[] regularResolvers, Resolver[] genericResolvers)
            //     {
            //         return (self, iface, name) => 
            //         {
            //             switch (iface.GUID.ToString())
            //             {
            //                 case "FCC22E95-66C7-4365-B8FA-510E623C0FCF": // regular
            //                     switch (name)
            //                     {
            //                         case "owned":
            //                             return regularResolvers[0](self, iface, name);
            //                         case "notowned":
            //                             return self.Parent != null
            //                                 ? self.Parent.GetEntry(iface, name)
            //                                 : regularResolvers[1](self, iface, name);
            //                     }
            //                     break;
            //                 case "DE67CA38-CD59-4506-8E12-AA32551A00E8": // generic
            //                     switch (name)
            //                     {
            //                         case null:
            //                             return genericResolvers[0](self, iface, name);
            //                     }
            //                     break;
            //             }
            //             return null;
            //         };
            //     }
            // }
            //

            public override Resolver Build(IEnumerable<AbstractServiceEntry> entries, ResolverCaseBuilder regularEntryResolverBuilder, ResolverCaseBuilder genericEntryResolverBuilder, out int reCount, out int geCount, CancellationToken cancellation = default)
            {
                List<Resolver>
                    regularEntryResolvers = new(),
                    genericEntryResolvers = new();

                CompilationUnitSyntax unit = CompilationUnit().WithMembers
                (
                    SingletonList<MemberDeclarationSyntax>
                    (
                        ClassDeclaration("ResolverFactory").WithModifiers
                        (
                            TokenList
                            (
                                new[]{ Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword) }
                            )
                        )
                        .WithMembers
                        (
                            SingletonList<MemberDeclarationSyntax>
                            (
                                CreateMainMethod
                                (
                                    entries, 
                                    entry =>
                                    {
                                        int index = regularEntryResolvers.Count;
                                        regularEntryResolvers.Add(regularEntryResolverBuilder(index, entry));
                                        return index;
                                    },
                                    entry =>
                                    {
                                        int index = genericEntryResolvers.Count;
                                        genericEntryResolvers.Add(genericEntryResolverBuilder(index, entry));
                                        return index;
                                    }
                                )
                            )
                        )
                    )
                );

                Debug.WriteLine(unit.NormalizeWhitespace(eol: Environment.NewLine).ToFullString());

                Compilation compilation = CSharpCompilation.Create
                (
                    assemblyName: $"Generated_{Guid.NewGuid():N}",
                    syntaxTrees: new[]
                    {
                        CSharpSyntaxTree.Create(unit)
                    },
                    references: GetRuntimeAssemblies()
                        .Append(typeof(AbstractServiceEntry).Assembly)
                        .Append(typeof(IDisposableEx).Assembly)
                        .Append(typeof(ResolverBuilder).Assembly)
                        .Select(asm => MetadataReference.CreateFromFile(asm.Location)),
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release)
                );

                reCount = regularEntryResolvers.Count;
                geCount = genericEntryResolvers.Count;

                using MemoryStream stm = new();

                EmitResult result = compilation.Emit(stm, cancellationToken: cancellation);

                Debug.WriteLine(string.Join(Environment.NewLine, result.Diagnostics));

                if (!result.Success)
                {
                    string failures = string.Join(Environment.NewLine, result
                        .Diagnostics
                        .Where(d => d.Severity == DiagnosticSeverity.Error));

#pragma warning disable CA2201 // Do not raise reserved exception types
                    throw new Exception(failures);
#pragma warning restore CA2201
                }

                stm.Seek(0, SeekOrigin.Begin);

                return (Resolver) AssemblyLoadContext
                    .Default
                    .LoadFromStream(stm)
                    .ExportedTypes
                    .Single()
                    .InvokeMember
                    (
                        MainMethod,
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, 
                        null, 
                        null, 
                        new object[] 
                        {
                            regularEntryResolvers.ToArray(),
                            genericEntryResolvers.ToArray()
                        }, 
                        CultureInfo.InvariantCulture
                    );
            }
        }
    }
}
