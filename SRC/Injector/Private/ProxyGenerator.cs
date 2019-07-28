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
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.DI.Internals
{
    using Proxy;

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
            if (src.IsGenericType) return src.GetGenericTypeDefinition().GetQualifiedName(name => GenericName(name).WithTypeArgumentList
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

            return src.GetQualifiedName();
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
                                    expression: ParenthesizedLambdaExpression
                                    (
                                        body: InvocationExpression
                                        (
                                            expression: MemberAccessExpression
                                            (
                                                kind: SyntaxKind.SimpleMemberAccessExpression,
                                                expression: IdentifierName(TARGET),
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
                            expression: ParenthesizedLambdaExpression
                            (
                                body: MemberAccessExpression
                                (
                                    kind: SyntaxKind.SimpleMemberAccessExpression,
                                    expression: IdentifierName(TARGET),
                                    name: IdentifierName(property.Name)
                                )
                            )
                        )
                    )
                )
            ));
        }

        internal static LocalDeclarationStatementSyntax CallInvoke(params ExpressionSyntax[] arguments) => DeclareLocal<object>("result", InvocationExpression
        (
            expression: IdentifierName(nameof(InterfaceInterceptor<IDisposable>.Invoke))
        )
        .WithArgumentList
        (
            argumentList: ArgumentList
            (
                arguments.CreateList(Argument)
            )
        ));

        internal static LocalDeclarationStatementSyntax CallInvoke(params LocalDeclarationStatementSyntax[] arguments) => CallInvoke(arguments.Select(arg => (ExpressionSyntax) arg.ToIdentifierName()).ToArray());

        internal static StatementSyntax CallTargetAndReturn(MethodInfo method)
        {
            InvocationExpressionSyntax invocation = InvocationExpression
            (
                expression: MemberAccessExpression
                (
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(TARGET),
                    IdentifierName(method.Name)
                )
            )
            .WithArgumentList
            (
                argumentList: ArgumentList(method.GetParameters().CreateList(param =>
                {
                    ArgumentSyntax argument = Argument
                    (
                        expression: IdentifierName(param.Name)
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
            );

            return method.ReturnType != typeof(void) 
                ? (StatementSyntax) ReturnStatement(invocation)
                : Block
                (
                    statements: List(new StatementSyntax[]{ExpressionStatement(invocation), ReturnStatement()})
                );
        }

        internal static StatementSyntax ReadTargetAndReturn(PropertyInfo property) => ReturnStatement
        (
            expression: MemberAccessExpression
            (
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName(TARGET),
                IdentifierName(property.Name)
            )
        );

        internal static StatementSyntax WriteTarget(PropertyInfo property) => ExpressionStatement
        (
            expression: AssignmentExpression
            (
                kind: SyntaxKind.SimpleAssignmentExpression,
                left: MemberAccessExpression
                (
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(TARGET),
                    IdentifierName(property.Name)
                ),
                right: IdentifierName(VALUE)
            )
        );

        internal static IfStatementSyntax ShouldCallTarget(LocalDeclarationStatementSyntax result, StatementSyntax ifTrue) => IfStatement
        (
            condition: BinaryExpression
            (
                kind: SyntaxKind.EqualsExpression, 
                left: result.ToIdentifierName(), 
                right: IdentifierName(CALL_TARGET)
            ),
            statement: ifTrue
        );

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

        internal static ConstructorDeclarationSyntax Ctor(ConstructorInfo ctor)
        {
            IReadOnlyList<ParameterInfo> paramz = ctor.GetParameters();

            return ConstructorDeclaration
            (
                identifier: Identifier(GeneratedClassName)
            )
            .WithModifiers
            (
                modifiers: TokenList
                (
                    Token(SyntaxKind.PublicKeyword)
                )
            )
            .WithParameterList
            (
                parameterList: ParameterList(paramz.CreateList(param => Parameter
                (
                    identifier: Identifier(param.Name)
                )
                .WithType
                (
                    type: CreateType(param.ParameterType)
                )))
            )
            .WithInitializer
            (
                initializer: ConstructorInitializer
                (
                    SyntaxKind.BaseConstructorInitializer,
                    ArgumentList(paramz.CreateList(param => Argument
                    (
                        expression: IdentifierName(param.Name)
                    )))
                )
            )
            .WithBody(Block());
        }
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
            //     MethodInfo currentMethod = MethodAccess(() => Target.Foo(para1, ref dummy_para2, out dummy_para3, para4)); // MethodBase.GetCurrentMethod() az implementaciot adna vissza, reflexio-val meg kibaszott lassu lenne
            //
            //     object result = Invoke(currentMethod, args);
            //     if (result == CALL_TARGET) return Target.Foo(para1, ref para2, out para3, para4); // void visszateresnel ures return
            //
            //     para2 = (T2) args[1];
            //     para3 = (T3) args[2];
            //
            //     return (TResult) result; // void visszateresnel nincs
            // }
            //

            LocalDeclarationStatementSyntax currentMethod, args, result;

            var statements = new List<StatementSyntax>()
                .Concat(AcquireMethodInfo(ifaceMethod, out currentMethod))
                .Append(args = CreateArgumentsArray(ifaceMethod))
                .Append(result = CallInvoke(currentMethod, args))
                .Append(ShouldCallTarget(result, ifTrue: CallTargetAndReturn(ifaceMethod)))
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
            //         PropertyInfo currentProperty = PropertyAccess(() => Target.Prop);
            //
            //         object result = Invoke(currentProperty.GetMethod, new object[0]);
            //         if (result == CALL_TARGET) return Target.Prop;
            //
            //         return (TResult) result;
            //     }
            //     set
            //     {
            //         PropertyInfo currentProperty = PropertyAccess(() => Target.Prop);
            //
            //         object result = Invoke(currentProperty.SetMethod, new object[]{ value });
            //         if (result == CALL_TARGET) Target.Prop = value;
            //     }
            // }
            //

            LocalDeclarationStatementSyntax currentProperty, result;

            return DeclareProperty
            (
                property: ifaceProperty,
                getBody: Block
                (
                    statements: new StatementSyntax[]
                    {
                        currentProperty = AcquirePropertyInfo(ifaceProperty),
                        result = CallInvoke
                        (
                            MemberAccessExpression // currentProperty.GetMethod
                            (
                                kind: SyntaxKind.SimpleMemberAccessExpression,
                                expression: currentProperty.ToIdentifierName(),
                                name: IdentifierName(nameof(PropertyInfo.GetMethod))
                            ),
                            CreateArray<object>() // new object[0]
                        ),
                        ShouldCallTarget(result, ifTrue: ReadTargetAndReturn(ifaceProperty)),
                        ReturnResult(ifaceProperty.PropertyType, result)
                    }
                ),
                setBody: Block
                (
                    statements: new StatementSyntax[]
                    {
                        currentProperty = AcquirePropertyInfo(ifaceProperty),
                        result = CallInvoke
                        (
                            MemberAccessExpression // currentProperty.SetMethod
                            (
                                kind: SyntaxKind.SimpleMemberAccessExpression,
                                expression: currentProperty.ToIdentifierName(),
                                name: IdentifierName(nameof(PropertyInfo.SetMethod))
                            ),
                            CreateArray<object>(IdentifierName(VALUE)) // new object[] {value}
                        ),
                        ShouldCallTarget(result, ifTrue: WriteTarget(ifaceProperty))
                    }
                )
            );
        }      

        public static MethodDeclarationSyntax PropertyAccess(Type interfaceType)
        {
            //
            // private static PropertyInfo PropertyAccess<TResult>(Expression<Func<TResult>> propertyAccess)
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
                    {paraName, typeof(Expression<>).MakeGenericType(typeof(Func<>).MakeGenericType(TResult))} // csak egyszer fut le interface-enkent -> nem kell gyorsitotarazni
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
            // private static MethodInfo MethodAccess(Expression<Action> methodAccess) 
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
                    {paraName, typeof(Expression<>).MakeGenericType(typeof(Action))} // csak egyszer fut le interface-enkent -> nem kell gyorsitotarazni
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
            Debug.Assert(interfaceType.IsInterface);

            ClassDeclarationSyntax cls = ClassDeclaration(GeneratedClassName)
                .WithModifiers
                (
                    modifiers: TokenList
                    (
                        //
                        // "SyntaxKind.PublicKeyword" ne szerepeljen h nem publikus osbol is leszarmazhassunk.
                        //

                        Token(SyntaxKind.InternalKeyword),
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

            List<MemberDeclarationSyntax> members = new List<MemberDeclarationSyntax>(new MemberDeclarationSyntax[]{Ctor(@base.GetApplicableConstructor()) });

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
            
            return cls.WithMembers(List(members));
        }

        public static CompilationUnitSyntax GenerateProxyUnit(Type @base, Type interfaceType)
        {
            CompilationUnitSyntax unit = CompilationUnit().WithMembers
            (
                members: SingletonList<MemberDeclarationSyntax>
                (
                    GenerateProxyClass(@base, interfaceType)
                )
            );

            IReadOnlyList<string> shouldIgnoreAccessCheck = new[] {@base, interfaceType}
                .Where(type => !type.IsVisible)
                .Select(type => type.Assembly.GetName().Name)
                .Distinct()
                .ToArray();
            if (shouldIgnoreAccessCheck.Any()) unit = unit.WithAttributeLists
            (
                SingletonList
                (
                    AttributeList
                    (
                        attributes: new[]{@base, interfaceType}
                            .Select(type => type.Assembly.GetName().Name)
                            .Distinct()
                            .ToArray() // ToArray() kell =(
                            .CreateList(CreateIgnoresAccessChecksToAttribute)
                    )
                    .WithTarget
                    (
                        AttributeTargetSpecifier(Token(SyntaxKind.AssemblyKeyword))
                    )
                )
            );

            return unit;

            AttributeSyntax CreateIgnoresAccessChecksToAttribute(string asm) => Attribute
            (
                typeof(IgnoresAccessChecksToAttribute).GetQualifiedName()
            )
            .WithArgumentList
            (
                argumentList: AttributeArgumentList
                (
                    arguments: SingletonSeparatedList
                    (
                        AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(asm)))
                    )
                )
            );
        }
        #endregion

        #region Private
        private static readonly string 
            CALL_TARGET = nameof(InterfaceInterceptor<IDisposable>.CALL_TARGET),
            TARGET = nameof(InterfaceInterceptor<IDisposable>.Target),
            // https://github.com/dotnet/roslyn/issues/4861
            VALUE = nameof(VALUE).ToLower();

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

        private static NameSyntax GetQualifiedName(this Type type, Func<string, NameSyntax> typeNameFactory = null)
        {
            return Parts2QualifiedName
            (
                parts: type.GetFriendlyName().Split('.').Reverse().ToArray(),
                factory: typeNameFactory ?? IdentifierName
            );

            NameSyntax Parts2QualifiedName(IReadOnlyCollection<string> parts, Func<string, NameSyntax> factory) => parts.Count == 1
                ? factory(parts.Single())
                : QualifiedName
                (
                    Parts2QualifiedName
                    (
                        parts: parts.Skip(1).ToArray(),
                        factory: IdentifierName
                    ),
                    (SimpleNameSyntax) factory(parts.First())
                );
        }
        #endregion
    }
}
