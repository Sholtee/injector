/********************************************************************************
* ProxyGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            // private static MethodInfo MethodAccess(Expression<Action<IInterface>> methodAccess) 
            // {
            //     return ((MethodCallExpression) methodAccess.Body).Method;
            // }
            //
            // TResult IInterface.Foo<TGeneric>(T1 para1, ref T2 para2, out T3 para3, TGeneric para4)
            // {
            //     object[] args = new object[] {para1, para2, default(T3), para4};
            //
            //     MethodInfo currentMethod = MethodAccess(i => i.Foo<TGeneric>(para1, ref para2, out para3, para4)); // MethodBase.GetCurrentMethod() az implementaciot adna vissza, reflexio-val meg kibaszott lassu lenne
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

        internal static void GenerateProxyProperty(PropertyInfo ifaceProperty)
        {
            //
            // private static PropertyInfo PropertyAccess<TResult>(Expression<Func<IInterface, TResult>> propertyAccess)
            // {
            //     return (PropertyInfo) ((MemberExpression) propertyAccess.Body).Member;
            // }
            //
            // TResult IInterface.Prop
            // {
            //     get 
            //     {
            //         PropertyInfo prop = GetProperty(i => i.Prop);
            //
            //         return (TResult) this.Invoke(prop.GetMethod, new object[0])
            //     }
            //     set
            //     {
            //         PropertyInfo prop = GetProperty(i => i.Prop);
            //
            //         this.Invoke(prop.SetMethod, new object[]{ value });
            //     }
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

        internal static MethodDeclarationSyntax DeclareMethod(
            Type returnType, 
            string name, 
            IReadOnlyList<SyntaxKind> modifiers, 
            IReadOnlyList<string> genericArguments, 
            IReadOnlyDictionary<string, Type> parameters)
        {
            MethodDeclarationSyntax result = MethodDeclaration
            (
                returnType: returnType != typeof(void)
                    ? CreateType(returnType)
                    : PredefinedType(Token(SyntaxKind.VoidKeyword)),
                identifier: Identifier(name)
            )
            .WithModifiers
            (
                modifiers: TokenList(modifiers.Select(Token))
            )

            .WithParameterList
            (
                parameterList: ParameterList
                (
                    parameters: parameters.CreateList(param => Parameter(Identifier(param.Key)).WithType
                    (
                        type: CreateType(param.Value)
                    ))
                )
            );

            if (genericArguments.Any()) result = result.WithTypeParameterList // kulon legyen kulomben lesz egy ures "<>"
            (
                typeParameterList: TypeParameterList
                (
                    parameters: genericArguments.CreateList(TypeParameter)
                )
            );

            return result;
        }

        internal static MethodDeclarationSyntax DeclareMethod(MethodInfo method)
        {
            Type 
                declaringType = method.DeclaringType,
                returnType    = method.ReturnType;

            MethodDeclarationSyntax result = MethodDeclaration
            (
                returnType: returnType != typeof(void) 
                    ? CreateType(returnType)
                    : PredefinedType(Token(SyntaxKind.VoidKeyword)),
                identifier: Identifier(method.Name)
            )
            .WithExplicitInterfaceSpecifier
            (
                explicitInterfaceSpecifier: ExplicitInterfaceSpecifier((NameSyntax) CreateType(declaringType))
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

            if (method.IsGenericMethod) result = result.WithTypeParameterList // kulon legyen kulomben lesz egy ures "<>"
            (
                typeParameterList: TypeParameterList
                (
                    parameters: method.GetGenericArguments().CreateList(type => TypeParameter(CreateType(type).ToFullString()))
                )
            );

            return result;
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
                        expression: AssignmentExpression
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

        internal static LocalDeclarationStatementSyntax AcquireMethodInfo(MethodInfo method)
        {
            const string i = nameof(i);

            return DeclareLocal<MethodInfo>("currentMethod", InvocationExpression
            (
                expression: IdentifierName(nameof(MethodAccess))
            )
            .WithArgumentList
            (
                argumentList: ArgumentList
                (
                    arguments: SingletonSeparatedList
                    (
                        Argument
                        (
                            expression: SimpleLambdaExpression
                            (
                                parameter: Parameter(Identifier(i)),
                                body: InvocationExpression
                                (
                                    expression: MemberAccessExpression
                                    (
                                        kind: SyntaxKind.SimpleMemberAccessExpression,
                                        expression: IdentifierName(i),
                                        name: IdentifierName(method.Name)
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
                            )
                        )
                    )
                )
            ));
        }

        internal static LocalDeclarationStatementSyntax CallInvoke(params LocalDeclarationStatementSyntax[] arguments) => DeclareLocal<object>("result", InvocationExpression
        (
            expression: MemberAccessExpression
            (
                kind: SyntaxKind.SimpleMemberAccessExpression,
                expression: ThisExpression(),
                name: IdentifierName(nameof(InterfaceProxy<int>.Invoke))
            )
        )
        .WithArgumentList
        (
            argumentList: ArgumentList
            (
                arguments.CreateList(arg => Argument
                (
                    expression: IdentifierName
                    (
                        arg.Declaration.Variables.Single().Identifier
                    )
                ))
            )
        ));

        internal static ReturnStatementSyntax ReturnResult(Type returnType, LocalDeclarationStatementSyntax result) => ReturnStatement
        (
            expression: returnType == typeof(void)
                ? null
                : CastExpression
                (
                    type: CreateType(returnType),
                    expression: IdentifierName
                    (
                        result.Declaration.Variables.Single().Identifier
                    )
                )
        );

        internal static MethodDeclarationSyntax PropertyAccess(Type interfaceType)
        {
            Debug.Assert(interfaceType.IsInterface);
            const string paraName = "propertyAccess";

            Type TResult = typeof(Func<>).GetGenericArguments().Single();  // ugly

            return DeclareMethod
            (
                returnType: typeof(PropertyInfo), 
                name: nameof(PropertyAccess), 
                modifiers: new []{ SyntaxKind.PrivateKeyword, SyntaxKind.StaticKeyword },
                genericArguments: new []{ TResult.Name },
                parameters: new Dictionary<string, Type>
                {
                    {paraName, typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(interfaceType, TResult))} // csak egyszer fut le interface-enkent -> nem kell gyorsitotarazni
                }
            )
            .WithBody
            (
                body: Block
                (
                    statements: SingletonList<StatementSyntax>
                    (
                        ReturnStatement
                        (
                            expression: CastMemberAccess<PropertyInfo>
                            (
                                expression: ParenthesizedExpression
                                (
                                    CastMemberAccess<MemberExpression>
                                    (
                                        expression: IdentifierName(paraName), 
                                        name: nameof(Expression<Action>.Body)
                                    )
                                ), 
                                name: nameof(MemberExpression.Member)
                            )
                        )
                    )
                )
            );

            ExpressionSyntax CastMemberAccess<TType>(ExpressionSyntax expression, string name) => CastExpression
            (
                type: CreateType<TType>(),
                expression: MemberAccessExpression
                (
                    kind: SyntaxKind.SimpleMemberAccessExpression,
                    expression: expression,
                    name: IdentifierName(name) 
                )
            );
        }

        internal static MethodDeclarationSyntax MethodAccess(Type interfaceType)
        {
            Debug.Assert(interfaceType.IsInterface);
            const string paraName = "methodAccess";

            return DeclareMethod
            (
                returnType: typeof(MethodInfo),
                name: nameof(MethodAccess),
                modifiers: new[] { SyntaxKind.PrivateKeyword, SyntaxKind.StaticKeyword },
                genericArguments: new string[0],
                parameters: new Dictionary<string, Type>
                {
                    {paraName, typeof(Expression<>).MakeGenericType(typeof(Action<>).MakeGenericType(interfaceType))} // csak egyszer fut le interface-enkent -> nem kell gyorsitotarazni
                }
            )
            .WithBody
            (
                body: Block
                (
                    statements: SingletonList<StatementSyntax>
                    (
                        ReturnStatement
                        (
                            expression: MemberAccessExpression
                            (
                                kind: SyntaxKind.SimpleMemberAccessExpression,
                                expression: ParenthesizedExpression
                                (
                                    expression: CastExpression
                                    (
                                        type: CreateType<MethodCallExpression>(),
                                        expression: MemberAccessExpression
                                        (
                                            kind: SyntaxKind.SimpleMemberAccessExpression,
                                            expression: IdentifierName(paraName),
                                            name: IdentifierName(nameof(Expression<Action>.Body))
                                        )
                                    ) 
                                ),
                                name: IdentifierName(nameof(MethodCallExpression.Method))
                            )
                        )
                    )
                )
            );
        }

        #region Private
        private static SeparatedSyntaxList<TNode> CreateList<T, TNode>(this IReadOnlyCollection<T> src, Func<T, TNode> factory) where TNode : SyntaxNode => SeparatedList<TNode>
        (
            nodesAndTokens: src.SelectMany((p, i) =>
            {
                var l = new List<SyntaxNodeOrToken> { factory(p) };
                if (i < src.Count - 1) l.Add(Token(SyntaxKind.CommaToken));

                return l;
            })
        );

        private static readonly Regex TypeNameReplacer = new Regex(@"\&|`\d+\[[\w,]+\]", RegexOptions.Compiled);

        private static string GetFriendlyName(this Type src)
        {
            Debug.Assert(!src.IsGenericType || src.IsGenericTypeDefinition);
            return TypeNameReplacer.Replace(src.ToString(), string.Empty);
        }

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
