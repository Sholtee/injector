﻿/********************************************************************************
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
        #region Internal
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

        internal static ArrayCreationExpressionSyntax CreateArray<T>(params ExpressionSyntax[] elements) => ArrayCreationExpression
        (
            type: ArrayType
            (
                elementType: CreateType<T>()
            )
            .WithRankSpecifiers
            (
                rankSpecifiers: SingletonList
                (
                    ArrayRankSpecifier(SingletonSeparatedList
                    (
                        elements.Any() ? OmittedArraySizeExpression() : (ExpressionSyntax) LiteralExpression
                        (
                            SyntaxKind.NumericLiteralExpression,
                            Literal(0)
                        )
                    ))
                )
            ),
            initializer: !elements.Any() ? null : InitializerExpression(SyntaxKind.ArrayInitializerExpression).WithExpressions
            (
                expressions: elements.CreateList(e => e)
            )
        );

        internal static LocalDeclarationStatementSyntax CreateArgumentsArray(MethodInfo method) => DeclareLocal<object[]>("args", CreateArray<object>(method
            .GetParameters()
            .Select(param => param.IsOut ? DefaultExpression(CreateType(param.ParameterType)) : (ExpressionSyntax) IdentifierName(param.Name))
            .ToArray()));

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

            Debug.Assert(declaringType.IsInterface());

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

        internal static PropertyDeclarationSyntax DeclareProperty(PropertyInfo property, BlockSyntax getBody, BlockSyntax setBody)
        {
            Debug.Assert(property.DeclaringType.IsInterface());

            PropertyDeclarationSyntax result = PropertyDeclaration
            (
                type: CreateType(property.PropertyType),
                identifier: Identifier(property.Name)
            )
            .WithExplicitInterfaceSpecifier
            (
                explicitInterfaceSpecifier: ExplicitInterfaceSpecifier((NameSyntax) CreateType(property.DeclaringType))
            );

            List<AccessorDeclarationSyntax> accessors = new List<AccessorDeclarationSyntax>();

            if (property.CanRead && getBody != null)  accessors.Add(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithBody(getBody));
            if (property.CanWrite && setBody != null) accessors.Add(AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithBody(setBody));

            return !accessors.Any() ? result : result.WithAccessorList
            (
                accessorList: AccessorList
                (
                    accessors: List(accessors)
                )
            );
        }

        internal static IReadOnlyList<ExpressionStatementSyntax> AssignByRefParameters(MethodInfo method, LocalDeclarationStatementSyntax argsArray)
        {
            IdentifierNameSyntax array = argsArray.ToIdentifierName();

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
            if (src.IsGenericType) return GetQualifiedName(src.GetGenericTypeDefinition(), name => GenericName(name).WithTypeArgumentList
            (
                typeArgumentList: TypeArgumentList
                (
                    arguments: src.GetGenericArguments().CreateList(CreateType)
                )
            ));

            if (src.IsArray) return ArrayType
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

            return GetQualifiedName(src, IdentifierName);

            NameSyntax GetQualifiedName(Type type, Func<string, NameSyntax> typeNameFactory) => Parts2QualifiedName
            (
                parts: type.GetFriendlyName().Split('.').Reverse().ToArray(),
                typeNameFactory: typeNameFactory
            );

            NameSyntax Parts2QualifiedName(IReadOnlyCollection<string> parts, Func<string, NameSyntax> typeNameFactory) => parts.Count == 1
                ? typeNameFactory(parts.Single())
                : QualifiedName
                (
                    Parts2QualifiedName
                    (
                        parts: parts.Skip(1).ToArray(), 
                        typeNameFactory: IdentifierName
                    ),
                    (SimpleNameSyntax) typeNameFactory(parts.First())
                );
        }

        internal static TypeSyntax CreateType<T>() => CreateType(typeof(T));

        internal static IReadOnlyList<LocalDeclarationStatementSyntax> AcquireMethodInfo(MethodInfo method, out LocalDeclarationStatementSyntax currentMethod)
        {
            const string i = nameof(i);

            IReadOnlyList<ParameterInfo> paramz = method.GetParameters();

            return paramz
                .Where(param => param.ParameterType.IsByRef)
                .Select(param => DeclareLocal
                (
                    type: param.ParameterType, 
                    name: GetDummyName(param), 
                    initializer: param.IsOut ? null : DefaultExpression
                    (
                        type: CreateType(param.ParameterType)
                    )                   
                ))
                .Append
                (
                    currentMethod = DeclareLocal<MethodInfo>(nameof(currentMethod), InvocationExpression
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
                                            argumentList: ArgumentList(paramz.CreateList(param =>
                                            {
                                                ArgumentSyntax argument = Argument
                                                (
                                                    expression: IdentifierName(param.ParameterType.IsByRef ? GetDummyName(param) : param.Name)
                                                );

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
                    )
                ))
                .ToArray();

            string GetDummyName(ParameterInfo param) => $"dummy_{param.Name}";
        }

        internal static LocalDeclarationStatementSyntax AcquirePropertyInfo(PropertyInfo property)
        {
            const string i = nameof(i);

            return DeclareLocal<PropertyInfo>("currentProperty", InvocationExpression
            (
                expression: IdentifierName(nameof(PropertyAccess))
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
                                body: MemberAccessExpression
                                (
                                    kind: SyntaxKind.SimpleMemberAccessExpression,
                                    expression: IdentifierName(i),
                                    name: IdentifierName(property.Name)
                                )
                            )
                        )
                    )
                )
            ));
        }

        internal static InvocationExpressionSyntax CallInvoke(params ExpressionSyntax[] arguments) => InvocationExpression
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
                arguments.CreateList(Argument)
            )
        );

        internal static InvocationExpressionSyntax CallInvoke(params LocalDeclarationStatementSyntax[] arguments) => CallInvoke(arguments.Select(arg => (ExpressionSyntax) arg.ToIdentifierName()).ToArray());

        internal static ReturnStatementSyntax ReturnResult(Type returnType, ExpressionSyntax result) => ReturnStatement
        (
            expression: returnType == typeof(void)
                ? null
                : CastExpression
                (
                    type: CreateType(returnType),
                    expression: result
                )
        );

        internal static ReturnStatementSyntax ReturnResult(Type returnType, LocalDeclarationStatementSyntax result) => ReturnResult(returnType, result.ToIdentifierName());
        #endregion

        #region Public
        public static MethodDeclarationSyntax GenerateProxyMethod(MethodInfo ifaceMethod)
        {
            //
            // TResult IInterface.Foo<TGeneric>(T1 para1, ref T2 para2, out T3 para3, TGeneric para4)
            // {
            //     object[] args = new object[] {para1, para2, default(T3), para4};
            //
            //     T2 dummy_para2 = default(T2); // ByRef metodus parameterek nem szerepelhetnek kifejezesekben
            //     T3 dummy_para3;
            //     MethodInfo currentMethod = MethodAccess(i => i.Foo<TGeneric>(para1, ref dummy_para2, out dummy_para3, para4)); // MethodBase.GetCurrentMethod() az implementaciot adna vissza, reflexio-val meg kibaszott lassu lenne
            //
            //     object result = this.Invoke(currentMethod, args);
            //     
            //     para2 = (T2) args[1];
            //     para3 = (T3) args[2];
            //
            //     return (TResult) result; // ifaceMethod.ReturnType != typeof(void)
            // }
            //
            
            LocalDeclarationStatementSyntax currentMethod, args, result;

            var statements = new List<StatementSyntax>()
                .Concat(AcquireMethodInfo(ifaceMethod, out currentMethod))
                .Append(args = CreateArgumentsArray(ifaceMethod))
                .Append(result = DeclareLocal<object>(nameof(result), CallInvoke(currentMethod, args)))
                .Concat(AssignByRefParameters(ifaceMethod, args));

            if (ifaceMethod.ReturnType != typeof(void)) statements = statements.Append(ReturnResult(ifaceMethod.ReturnType, result));

            return DeclareMethod(ifaceMethod).WithBody
            (
                body: Block
                (
                    statements: List(statements)
                )
            );
        }

        public static PropertyDeclarationSyntax GenerateProxyProperty(PropertyInfo ifaceProperty)
        {
            //
            // TResult IInterface.Prop
            // {
            //     get 
            //     {
            //         PropertyInfo currentProperty = PropertyAccess(i => i.Prop);
            //
            //         return (TResult) this.Invoke(currentProperty.GetMethod, new object[0])
            //     }
            //     set
            //     {
            //         PropertyInfo currentProperty = PropertyAccess(i => i.Prop);
            //
            //         this.Invoke(currentProperty.SetMethod, new object[]{ value });
            //     }
            // }
            //

            LocalDeclarationStatementSyntax currentProperty;

            return DeclareProperty
            (
                property: ifaceProperty,
                getBody: Block
                (
                    statements: new StatementSyntax[]
                    {
                        currentProperty = AcquirePropertyInfo(ifaceProperty),
                        ReturnResult(ifaceProperty.PropertyType, CallInvoke
                        (
                            MemberAccessExpression // currentProperty.GetMethod
                            (
                                kind: SyntaxKind.SimpleMemberAccessExpression,
                                expression: currentProperty.ToIdentifierName(),
                                name: IdentifierName(nameof(PropertyInfo.GetMethod))
                            ),
                            CreateArray<object>() // new object[0]
                        ))
                    }
                ),
                setBody: Block
                (
                    statements: new StatementSyntax[]
                    {
                        currentProperty = AcquirePropertyInfo(ifaceProperty),
                        ExpressionStatement(CallInvoke
                        (
                            MemberAccessExpression // currentProperty.SetMethod
                            (
                                kind: SyntaxKind.SimpleMemberAccessExpression,
                                expression: currentProperty.ToIdentifierName(),
                                name: IdentifierName(nameof(PropertyInfo.SetMethod))
                            ),
                            // TODO: FIXME: nem string-kent szerepeljen a "value"
                            CreateArray<object>(IdentifierName("value")) // new object[] {value}
                        ))
                    }
                )
            );
        }      

        public static MethodDeclarationSyntax PropertyAccess(Type interfaceType)
        {
            //
            // private static PropertyInfo PropertyAccess<TResult>(Expression<Func<IInterface, TResult>> propertyAccess)
            // {
            //     return (PropertyInfo) ((MemberExpression) propertyAccess.Body).Member;
            // }
            //

            Debug.Assert(interfaceType.IsInterface());
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

        public static MethodDeclarationSyntax MethodAccess(Type interfaceType)
        {
            //
            // private static MethodInfo MethodAccess(Expression<Action<IInterface>> methodAccess) 
            // {
            //     return ((MethodCallExpression) methodAccess.Body).Method;
            // }
            //

            Debug.Assert(interfaceType.IsInterface());
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

        public const string GeneratedClassName = "GeneratedProxy";

        public static ClassDeclarationSyntax GenerateProxyClass(Type @base, Type interfaceType)
        {
            Debug.Assert(typeof(InterfaceInterceptor).IsAssignableFrom(@base));
            Debug.Assert(interfaceType.IsInterface);

            ClassDeclarationSyntax cls = ClassDeclaration(GeneratedClassName)
                .WithModifiers
                (
                    modifiers: TokenList
                    (
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.SealedKeyword)
                    )
                )
                .WithBaseList
                (
                    baseList: BaseList
                    (
                        new[] {@base, interfaceType}.CreateList<Type, BaseTypeSyntax>(t => SimpleBaseType(CreateType(t)))
                    )
                );

            List<MemberDeclarationSyntax> members = new List<MemberDeclarationSyntax>();

            MethodInfo[] methods = interfaceType.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(method => !method.IsSpecialName).ToArray();
            if (methods.Any())
            {
                members.Add(MethodAccess(interfaceType));
                members.AddRange(methods.Select(GenerateProxyMethod));
            }

            PropertyInfo[] properties = interfaceType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            if (properties.Any())
            {
                members.Add(PropertyAccess(interfaceType));
                members.AddRange(properties.Select(GenerateProxyProperty));
            }

            if (members.Any()) cls = cls.WithMembers(List(members));
            return cls;
        }
        #endregion

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

        private static IdentifierNameSyntax ToIdentifierName(this LocalDeclarationStatementSyntax variable) => IdentifierName(variable.Declaration.Variables.Single().Identifier);

        private static readonly Regex TypeNameReplacer = new Regex(@"\&|`\d+\[[\w,]+\]", RegexOptions.Compiled);

        private static string GetFriendlyName(this Type src)
        {
            Debug.Assert(!src.IsGenericType() || src.IsGenericTypeDefinition());
            return TypeNameReplacer.Replace(src.ToString(), string.Empty);
        }
        #endregion
    }
}
