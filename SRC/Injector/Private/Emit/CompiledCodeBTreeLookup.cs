/********************************************************************************
* CompiledCodeBTreeLookup.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.DI.Internals
{
    using Primitives;

    internal sealed partial class CompiledCodeBTreeLookup<TData>
    {
        private static readonly Regex TypeNameReplacer = new(@"\&|\*|`\d+(\[[\w,<>]+\])?", RegexOptions.Compiled);

        private static NameSyntax GetQualifiedName(Type type)
        {
            string[] parts = GetFriendlyName(type).Split(Type.Delimiter);

            List<NameSyntax> names = new(parts.Length);

            for (int i = 0; i < parts.Length - 1; i++)
            {
                names[i] = IdentifierName(parts[i]);
            }

            names[names.Count - 1] = !type.IsGenericTypeDefinition ? IdentifierName(parts[parts.Length - 1]) : GenericName(parts[parts.Length - 1]).WithTypeArgumentList
            (
                TypeArgumentList
                (
                    ToCommaSeparatedList
                    (
                        type.GetGenericArguments(),
                        ResolveType
                    )
                )
            );

            if (type != typeof(void) && !type.IsGenericParameter) names[0] = AliasQualifiedName
            (
                IdentifierName
                (
                    Token(SyntaxKind.GlobalKeyword)
                ),
                (SimpleNameSyntax) names[0]
            );

            return Qualify(names);

            static NameSyntax Qualify(List<NameSyntax> parts)
            {
                if (parts.Count is 0)
                    throw new InvalidOperationException();

                if (parts.Count is 1)
                    return parts[0];

                return QualifiedName
                (
                    left: Qualify(parts.GetRange(0, parts.Count - 1)),
                    right: (SimpleNameSyntax)parts[parts.Count - 1]
                );
            }

            static string GetFriendlyName(Type src)
            {
                if (src.IsConstructedGenericType)
                    src = src.GetGenericTypeDefinition();

                return TypeNameReplacer.Replace
                (
                    src.ToString(),
                    string.Empty
                );
            }
        }

        private static SeparatedSyntaxList<TDest> ToCommaSeparatedList<TSource, TDest>(IEnumerable<TSource> src, Func<TSource, TDest> convert) where TDest : SyntaxNode
        {
            List<SyntaxNodeOrToken> nodesAndTokens = new();

            foreach (TSource item in src)
            {
                if (nodesAndTokens.Count > 0)
                    nodesAndTokens.Add
                    (
                        Token(SyntaxKind.CommaToken)
                    );

                nodesAndTokens.Add
                (
                    convert(item)
                );
            }

            return SeparatedList<TDest>(nodesAndTokens);
        }

        private static TypeSyntax ResolveType(Type type)
        {
            if (type == typeof(void))
            {
                return PredefinedType
                (
                    Token(SyntaxKind.VoidKeyword)
                );
            }

            if (type.IsArray)
            {
                return ArrayType
                (
                    elementType: ResolveType
                    (
                        type.GetElementType()
                    )
                )
                .WithRankSpecifiers
                (
                    rankSpecifiers: SingletonList
                    (
                        node: ArrayRankSpecifier
                        (
                            sizes: ToCommaSeparatedList
                            (
                                Enumerable
                                    .Repeat
                                    (
                                        0, 
                                        type.GetArrayRank()
                                    )
                                    .Select
                                    (
                                        static _ => OmittedArraySizeExpression()
                                    ),
                                static arSize => (ExpressionSyntax)arSize
                            )
                        )
                    )
                );
            }

            return GetQualifiedName(type);
        }

        //
        // public static Func<CompositeKey, TData> CreateFactory(KeyValuePair<CompositeKey, TData>[] array) => key =>
        // {
        //     int order;
        //     ...
        // }
        //

        public MethodDeclarationSyntax BuildFactory(out object? closures)
        {
            List<RedBlackTreeNode<KeyValuePair<CompositeKey, TData>>> nodeLst = new();

            IdentifierNameSyntax
                array = IdentifierName(nameof(array)),
                key   = IdentifierName(nameof(key)),
                order = IdentifierName(nameof(order));

            MethodDeclarationSyntax result = MethodDeclaration
            (
                ResolveType(typeof(Func<CompositeKey, TData>)),
                Identifier($"CreateFactory_{Guid.NewGuid():N}")
            )
            .WithModifiers
            (
                TokenList
                (
                    new[]
                    {
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.StaticKeyword)
                    }
                )
            )
            .WithParameterList
            (
                ParameterList
                (
                    SingletonSeparatedList
                    (
                        Parameter(array.Identifier).WithType
                        (
                            ResolveType
                            (
                                typeof(KeyValuePair<CompositeKey, TData>[])
                            )
                        )
                    )
                )
            )
            .WithExpressionBody
            (
                ArrowExpressionClause
                (
                    ParenthesizedLambdaExpression().WithParameterList
                    (
                        ParameterList
                        (
                            SingletonSeparatedList
                            (
                                Parameter(key.Identifier).WithType
                                (
                                    ResolveType(typeof(CompositeKey))
                                )
                            )
                        )
                    )
                    .WithBlock
                    (
                        Block
                        (
                            GetBody()
                        )
                    )
                )
            )
            .WithSemicolonToken
            (
                Token(SyntaxKind.SemicolonToken)
            );

            closures = nodeLst.ToArray();
            return result;

            IEnumerable<StatementSyntax> GetBody()
            {
                yield return LocalDeclarationStatement
                (
                    VariableDeclaration
                    (
                        PredefinedType
                        (
                            Token(SyntaxKind.IntKeyword)
                        )
                    )
                    .WithVariables
                    (
                        SingletonSeparatedList
                        (
                            VariableDeclarator(order.Identifier)
                        )
                    )
                );

                foreach (StatementSyntax statement in BuildNode(FTree.Root, key, array, order, nodeLst))
                {
                    yield return statement;
                }
            }
        }

        private static IEnumerable<StatementSyntax> BuildNode(RedBlackTreeNode<KeyValuePair<CompositeKey, TData>>? node, IdentifierNameSyntax key, IdentifierNameSyntax array, IdentifierNameSyntax order, IList<RedBlackTreeNode<KeyValuePair<CompositeKey, TData>>> nodes)
        {
            if (node is null)
            {
                yield return ReturnStatement
                (
                    LiteralExpression(SyntaxKind.NullLiteralExpression)
                );
                yield break;
            }

            nodes.Add(node);

            int i = nodes.Count - 1;  // store here since the recursive calls will change the count

            //
            // order = key.CompareTo(array[index].Key);
            //

            yield return ExpressionStatement
            (
                AssignmentExpression
                (
                    SyntaxKind.SimpleAssignmentExpression,
                    order,
                    InvocationExpression
                    (
                        MemberAccessExpression
                        (
                            SyntaxKind.SimpleMemberAccessExpression,
                            key,
                            IdentifierName
                            (
                                nameof(CompositeKey.CompareTo)
                            )
                        )
                    )
                    .WithArgumentList
                    (
                        ArgumentList
                        (
                            SingletonSeparatedList
                            (
                                Argument
                                (
                                    MemberAccessExpression
                                    (
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        ElementAccessExpression(array).WithArgumentList
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
                                                            Literal(i)
                                                        )
                                                    )
                                                )
                                            )
                                        ),
                                        IdentifierName
                                        (
                                            nameof(KeyValuePair<CompositeKey, TData>.Key)
                                        )
                                    )
                                )
                            )
                        )
                    )
                )
            );

            //
            //  if (order < 0) { ... }
            //

            yield return IfStatement
            (
                BinaryExpression
                (
                    SyntaxKind.LessThanExpression,
                    order,
                    LiteralExpression
                    (
                        SyntaxKind.NumericLiteralExpression,
                        Literal(0)
                    )
                ),
                Block
                (
                    BuildNode(node.Left, key, array, order, nodes)
                )
            );

            //
            //  if (order > 0) { ... }
            //

            yield return IfStatement
            (
                BinaryExpression
                (
                    SyntaxKind.GreaterThanExpression,
                    order,
                    LiteralExpression
                    (
                        SyntaxKind.NumericLiteralExpression,
                        Literal(0)
                    )
                ),
                Block
                (
                    BuildNode(node.Right, key, array, order, nodes)
                )
            );

            //
            // return array[index].Value;
            //

            yield return ReturnStatement
            (
                MemberAccessExpression
                (
                    SyntaxKind.SimpleMemberAccessExpression,
                    ElementAccessExpression(array).WithArgumentList
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
                                        Literal(i)
                                    )
                                )
                            )
                        )
                    ),
                    IdentifierName
                    (
                        nameof(KeyValuePair<CompositeKey, TData>.Value)
                    )
                )
            );
        }
    }
}
