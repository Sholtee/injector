/********************************************************************************
* ProxyGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.DI.Internals
{
    internal static class ProxyGenerator
    {
        public static void GenerateProxyMethod(MethodInfo ifaceMethod)
        {
            //
            // TResult IInterface.Foo<TGeneric>(T1 para1, ref T2 para2, out T3 para3, TGeneric para4)
            // {
            //     object[] args = new object[] {para1, para2, default(T3), para4};
            //
            //     Expression<Action<IInterface>> callExpr = () => i.Foo<TGeneric>(para1, ref para2, out para3, para4);
            //     MethodInfo currentMethod = ((MethodCallExpression) callExpr.Body).Method; // MethodBase.GetCurrentMethod() lassu, nincs netcore1_X-ben es a generikus definiciot adna vissza
            //
            //     object result = this.Invoke(currentMethod, args);
            //     
            //     para2 = args[1];
            //     para3 = args[2];
            //
            //     return (TResult) result; // ifaceMethod.ReturnType != typeof(void)
            // }
            //
        }

        public static LocalDeclarationStatementSyntax DeclareLocal<T>(string name) => LocalDeclarationStatement
        (
            declaration: VariableDeclaration
            (
                type:      IdentifierName(typeof(T).FullName),
                variables: SeparatedList(new List<VariableDeclaratorSyntax>
                {
                    VariableDeclarator
                    (
                        identifier: name.ToIdentifier()
                    )
                })
            )
        ).NormalizeWhitespace();

        public static ArrayCreationExpressionSyntax CreateArgumentsArray(MethodInfo method) => ArrayCreationExpression
        (
            type: ArrayType
            (
                elementType: typeof(object[]).ToIdentifierName()                  
            ),
            initializer: InitializerExpression(SyntaxKind.ArrayInitializerExpression).WithExpressions
            (
                expressions: method
                    .GetParameters()
                    .CreateList(param => param.IsOut ? DefaultExpression(param.ParameterType.ToIdentifierName()) : (ExpressionSyntax) IdentifierName(param.Name))
            )
        ).NormalizeWhitespace();

        public static MethodDeclarationSyntax DeclareMethod(MethodInfo method) => MethodDeclaration
        (
            returnType: method.ReturnType != typeof(void) 
                ? (TypeSyntax) method.ReturnType.ToIdentifierName()
                : (TypeSyntax) PredefinedType(Token(SyntaxKind.VoidKeyword)),
            identifier: method.Name.ToIdentifier() // ne siman a metoduson hivjuk a ToIdentifier()
        )
        .WithModifiers
        (
            modifiers: TokenList(Token(SyntaxKind.PublicKeyword))
        )
        .WithTypeParameterList
        (
            typeParameterList: TypeParameterList
            (
                parameters: method.GetGenericArguments().CreateList(type => TypeParameter(type.ToIdentifier()))
            )
        )
        .WithParameterList
        (
            ParameterList
            (
                parameters: method.GetParameters().CreateList(param =>
                {
                    ParameterSyntax parameter = Parameter(param.Name.ToIdentifier()).WithType // ne siman a param-on hivjuk a ToIdentifier()-t
                    (
                        type: param.ParameterType.ToIdentifierName()
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
        )
        .NormalizeWhitespace();

        private static SeparatedSyntaxList<TNode> CreateList<T, TNode>(this IReadOnlyList<T> src, Func<T, TNode> factory) where TNode : SyntaxNode => SeparatedList<TNode>
        (
            nodesAndTokens: src.SelectMany((p, i) =>
            {
                var l = new List<SyntaxNodeOrToken> { factory(p) };
                if (i < src.Count - 1) l.Add(Token(SyntaxKind.CommaToken));

                return l;
            })
        );

        private static IdentifierNameSyntax ToIdentifierName<T>(this T src) => IdentifierName(src
            .ToString() // tipus eseten "type.FullName ?? type.Name"-el egyenerteku
            .Replace("&", string.Empty)); // HACK: typeof(String).ToString() == "System.String?" de csak string-nel, MSDN peldaban pedig "System.String" szerepel

        private static SyntaxToken ToIdentifier<T>(this T src) => Identifier(src.ToString());
    }
}
