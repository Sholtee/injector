/********************************************************************************
* ProxyGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.DI.Internals
{
    internal static class ProxyGenerator
    {
        internal static void GenerateProxyMethod(MethodInfo ifaceMethod)
        {
            //
            // TResult IInterface.Foo<TGeneric>(T1 para1, ref T2 para2, out T3 para3, TGeneric para4)
            // {
            //     object[] args = new object[] {para1, para2, default(T3), para4};
            //
            //     Expression<Action<IInterface>> callExpr = i => i.Foo<TGeneric>(para1, ref para2, out para3, para4);
            //     MethodInfo currentMethod = ((MethodCallExpression) callExpr.Body).Method; // MethodBase.GetCurrentMethod() lassu, nincs netcore1_X-ben es a generikus definiciot adna vissza
            //
            //     object result = this.Invoke(currentMethod, args);
            //     
            //     para2 = (T2) args[1];
            //     para3 = (T3) args[2];
            //
            //     return (TResult) result; // ifaceMethod.ReturnType != typeof(void)
            // }
            //
        }

        internal static LocalDeclarationStatementSyntax DeclareLocal(Type type, string name, ExpressionSyntax initializer = null)
        {
            VariableDeclaratorSyntax declarator = VariableDeclarator
            (
                identifier: Identifier(name)
            );

            if (initializer != null) declarator = declarator.WithInitializer
            (
                initializer: EqualsValueClause(initializer)
            );

            return LocalDeclarationStatement
            (
                declaration: VariableDeclaration
                (
                    type: CreateType(type),
                    variables: SeparatedList(new List<VariableDeclaratorSyntax>
                    {
                        declarator
                    })
                )
            );
        }

        internal static LocalDeclarationStatementSyntax DeclareLocal<T>(string name, ExpressionSyntax initializer = null) => DeclareLocal(typeof(T), name, initializer);

        internal static ArrayCreationExpressionSyntax CreateArgumentsArray(MethodInfo method) => ArrayCreationExpression
        (
            type: ArrayType
            (
                elementType: CreateType<object[]>()                  
            ),
            initializer: InitializerExpression(SyntaxKind.ArrayInitializerExpression).WithExpressions
            (
                expressions: method
                    .GetParameters()
                    .CreateList(param => param.IsOut ? DefaultExpression(CreateType(param.ParameterType)) : (ExpressionSyntax) IdentifierName(param.Name))
            )
        );

        internal static MethodDeclarationSyntax DeclareMethod(MethodInfo method)
        {
            Type declaringType = method.DeclaringType;

            MethodDeclarationSyntax result = MethodDeclaration
            (
                returnType: method.ReturnType != typeof(void) 
                    ? CreateType(method.ReturnType)
                    : PredefinedType(Token(SyntaxKind.VoidKeyword)),
                identifier: Identifier(method.Name)
            );

            return 
            (
                declaringType.IsInterface
                    ? result.WithExplicitInterfaceSpecifier
                    (
                        explicitInterfaceSpecifier: ExplicitInterfaceSpecifier((NameSyntax) CreateType(declaringType))
                    )
                    : result.WithModifiers // tesztekhez
                    (
                        modifiers: TokenList(Token(SyntaxKind.PublicKeyword))
                    )
            )
            .WithTypeParameterList
            (
                typeParameterList: TypeParameterList
                (
                    parameters: method.GetGenericArguments().CreateList(type => TypeParameter(CreateType(type).ToFullString()))
                )
            )
            .WithParameterList
            (
                ParameterList
                (
                    parameters: method.GetParameters().CreateList(param =>
                    {
                        ParameterSyntax parameter = Parameter(Identifier(param.Name)).WithType
                        (
                            type: CreateType(param.ParameterType)
                        );

                        if (param.IsOut) parameter = parameter.WithModifiers
                        (
                            modifiers: TokenList(Token(SyntaxKind.OutKeyword))
                        );

                        //
                        // "ParameterType.IsByRef" param.IsOut eseten is igazat ad vissza -> IsOut teszt utan szerepeljen.
                        //

                        else if (param.ParameterType.IsByRef) parameter = parameter.WithModifiers
                        (
                            modifiers: TokenList(Token(SyntaxKind.RefKeyword))
                        );
     
                        return parameter;
                    })
                )
            );
        }

        internal static IReadOnlyList<ExpressionStatementSyntax> AssignByRefParameters(MethodInfo method, LocalDeclarationStatementSyntax argsArray)
        {
            IdentifierNameSyntax array = IdentifierName(argsArray.Declaration.Variables.Single().Identifier);

            return method
                .GetParameters()
                .Select((param, i) => new {Parameter = param, Index = i})
                .Where(p => p.Parameter.ParameterType.IsByRef)
                .Select
                (
                    p => ExpressionStatement
                    (
                        AssignmentExpression
                        (
                            kind:  SyntaxKind.SimpleAssignmentExpression, 
                            left:  IdentifierName(p.Parameter.Name),
                            right: CastExpression
                            (
                                type: CreateType(p.Parameter.ParameterType),
                                expression: ElementAccessExpression(array).WithArgumentList
                                (
                                    argumentList: BracketedArgumentList
                                    (
                                        SingletonSeparatedList(Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(p.Index))))
                                    )
                                )
                            )
                        )
                    )
                )
                .ToList();
        }

        internal static LocalDeclarationStatementSyntax SelfCallExpression(MethodInfo method)
        {
            const string i = nameof(i);

            Type declaringType = method.DeclaringType;
            
            //
            // Expression<Action<IInterface>>
            //
            // TODO: erre a gyorsitotar bejegyzesre csak a proxy felepiteseig van szukseg.
            //

            Type expressionType = Cache<Type, Type>.GetOrAdd(declaringType, () => typeof(Expression<>).MakeGenericType(typeof(Action<>).MakeGenericType(declaringType)));

            //
            // Expression<Action<IInterface>> callExpr = i => i.Foo(...)
            //

            return DeclareLocal(expressionType, "callExpr", SimpleLambdaExpression
            (
                parameter: Parameter(Identifier(i)),
                body: InvocationExpression
                (
                    expression: MemberAccessExpression
                    (
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(i),
                        IdentifierName(method.Name)
                    )
                )
                .WithArgumentList
                (
                    argumentList: ArgumentList(method.GetParameters().CreateList(param =>
                    {
                        ArgumentSyntax argument = Argument(IdentifierName(param.Name));

                        //
                        // TODO: "IN"
                        //

                        if (param.ParameterType.IsByRef) argument = argument.WithRefKindKeyword
                        (
                            refKindKeyword: Token(param.IsOut ? SyntaxKind.OutKeyword : SyntaxKind.RefKeyword)
                        );

                        return argument;
                    }))
                )
            ));
        }

        internal static TypeSyntax CreateType(Type src)
        {
            if (src.IsGenericType) return GenericName
            (
                identifier: Identifier(src.GetGenericTypeDefinition().GetFriendlyName())
            )
            .WithTypeArgumentList
            (
                typeArgumentList: TypeArgumentList
                (
                    arguments: src.GetGenericArguments().CreateList(CreateType)
                )
            );

            if (src.IsArray) ArrayType
            (
                elementType: CreateType(src.GetElementType())
            )
            .WithRankSpecifiers
            (
                rankSpecifiers: SingletonList
                (
                    node: ArrayRankSpecifier
                    (
                        //
                        // TODO: kezelje az int[10]-t
                        //

                        sizes: Enumerable.Repeat(0, src.GetArrayRank()).ToList().CreateList(@void => (ExpressionSyntax) OmittedArraySizeExpression())
                    )
                )
            );

            return IdentifierName(src.GetFriendlyName());
        }

        internal static TypeSyntax CreateType<T>() => CreateType(typeof(T));

        internal static LocalDeclarationStatementSyntax AcquireMethodInfo(LocalDeclarationStatementSyntax selfCallExpression) => DeclareLocal<MethodInfo>("currentMethod", MemberAccessExpression
        (
            kind: SyntaxKind.SimpleMemberAccessExpression,
            expression: ParenthesizedExpression
            (
                expression: CastExpression
                (
                    CreateType<MethodCallExpression>(),
                    MemberAccessExpression
                    (
                        kind: SyntaxKind.SimpleMemberAccessExpression,
                        expression: IdentifierName
                        (
                            selfCallExpression.Declaration.Variables.Single().Identifier.Text
                        ),
                        name: IdentifierName(nameof(Expression<Action>.Body))
                    )
                )
            ),
            name: IdentifierName(nameof(MethodCallExpression.Method))
        ));

        #region Private
        private static SeparatedSyntaxList<TNode> CreateList<T, TNode>(this IReadOnlyList<T> src, Func<T, TNode> factory) where TNode : SyntaxNode => SeparatedList<TNode>
        (
            nodesAndTokens: src.SelectMany((p, i) =>
            {
                var l = new List<SyntaxNodeOrToken> { factory(p) };
                if (i < src.Count - 1) l.Add(Token(SyntaxKind.CommaToken));

                return l;
            })
        );

        private static readonly Regex TypeNameReplacer = new Regex(@"\&|`\d+\[[\w,]+\]", RegexOptions.Compiled);

        private static string GetFriendlyName(this Type src) => TypeNameReplacer.Replace(src.ToString(), string.Empty);

        private static void PreCheck(Type type)
        {
            if (!type.IsInterface) throw null;
            if (type.IsNested) throw null;
            if (!type.IsPublic) throw null;
            if (type.ContainsGenericParameters) throw null;
        }
        #endregion
    }
}
