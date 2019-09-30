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

#if IGNORE_VISIBILITY
using System.Runtime.CompilerServices;
#endif

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.DI.Internals
{
    using Proxy;
    using static ProxyGeneratorBase;

    internal static class ProxyGenerator<TInterface, TInterceptor> where TInterface : class where TInterceptor: InterfaceInterceptor<TInterface>
    {
        private static readonly string
            CALL_TARGET = nameof(InterfaceInterceptor<TInterface>.CALL_TARGET),
            TARGET      = nameof(InterfaceInterceptor<TInterface>.Target);

        #region Internal
        internal static LocalDeclarationStatementSyntax CreateArgumentsArray(MethodInfo method) => DeclareLocal<object[]>("args", CreateArray<object>(method
            .GetParameters()
            .Select(param => param.IsOut ? DefaultExpression(CreateType(param.ParameterType)) : (ExpressionSyntax) IdentifierName(param.Name))
            .ToArray()));

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
                                        body: Invoke
                                        (
                                            method: method, 
                                            target: TARGET,

                                            //
                                            // GetDummyName() azert kell mert hivatkozott parameterek nem szerepelhetnek kifejezesekben.
                                            //

                                            arguments: paramz.Select(param => param.ParameterType.IsByRef ? GetDummyName(param) : param.Name).ToArray()
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
                right: IdentifierName(Value)
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
            //     object result = Invoke(currentMethod, args, currentMethod);
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
                .Append(result = CallInvoke(currentMethod, args, currentMethod))
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
            //         object result = Invoke(currentProperty.GetMethod, new object[0], currentProperty);
            //         if (result == CALL_TARGET) return Target.Prop;
            //
            //         return (TResult) result;
            //     }
            //     set
            //     {
            //         PropertyInfo currentProperty = PropertyAccess(() => Target.Prop);
            //
            //         object result = Invoke(currentProperty.SetMethod, new object[]{ value }, currentProperty);
            //         if (result == CALL_TARGET) Target.Prop = value;
            //     }
            // }
            //

            LocalDeclarationStatementSyntax currentProperty, result;

            //
            // Nem gond ha mondjuk az interface property-nek nincs gettere, akkor a "getBody"
            // figyelmen kivul lesz hagyva.
            //

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
                            CreateArray<object>(), // new object[0],
                            currentProperty.ToIdentifierName() // currentProperty
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
                            CreateArray<object>(IdentifierName(Value)), // new object[] {value}
                            currentProperty.ToIdentifierName() // currentProperty
                        ),
                        ShouldCallTarget(result, ifTrue: WriteTarget(ifaceProperty))
                    }
                )
            );
        }

        public static IReadOnlyList<MemberDeclarationSyntax> GenerateProxyEvent(EventInfo @event)
        {
            //
            // private static readonly EventInfo FEvent = GetEvent("Event");
            //
            // event EventType IInterface.Event
            // {
            //     add 
            //     {
            //         object result = Invoke(FEvent.AddMethod, new object[]{ value }, FEvent);
            //         if (result == CALL_TARGET) Target.Event += value;
            //     }
            //     remove
            //     {
            //         object result = Invoke(FEvent.RemoveMethod, new object[]{ value }, FEvent);
            //         if (result == CALL_TARGET) Target.Event -= value;
            //     }
            // }
            //

            string fieldName = $"F{@event.Name}";

            LocalDeclarationStatementSyntax result;

            return new MemberDeclarationSyntax[]
            {
                DeclareField<EventInfo>
                (
                    name: fieldName, 
                    initializer: InvocationExpression
                    (
                        expression: IdentifierName(nameof(GetEvent))
                    )
                    .WithArgumentList
                    (
                        argumentList: ArgumentList
                        (
                            SingletonSeparatedList
                            (
                                Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(@event.Name)))
                            )
                        )
                    ),
                    modifiers: new []
                    {
                        SyntaxKind.PrivateKeyword,
                        SyntaxKind.StaticKeyword,
                        SyntaxKind.ReadOnlyKeyword
                    }
                ),
                DeclareEvent
                (
                    @event: @event,
                    addBody: Block
                    (
                        statements: new StatementSyntax[]
                        {
                            result = CallInvoke
                            (
                                MemberAccessExpression // FEvent.AddMethod
                                (
                                    kind: SyntaxKind.SimpleMemberAccessExpression,
                                    expression: IdentifierName(fieldName),
                                    name: IdentifierName(nameof(EventInfo.AddMethod))
                                ),
                                CreateArray<object>(IdentifierName(Value)), // new object[] {value}
                                IdentifierName(fieldName) //FEvent
                            ),
                            ShouldCallTarget(result, ifTrue: ExpressionStatement
                            (
                                expression: RegisterEvent(@event, TARGET, add: true))
                            )
                        }
                    ),
                    removeBody: Block
                    (
                        statements: new StatementSyntax[]
                        {
                            result = CallInvoke
                            (
                                MemberAccessExpression
                                (
                                    kind: SyntaxKind.SimpleMemberAccessExpression,
                                    expression: IdentifierName(fieldName),
                                    name: IdentifierName(nameof(EventInfo.RemoveMethod))
                                ),
                                CreateArray<object>(IdentifierName(Value)),
                                IdentifierName(fieldName)
                            ),
                            ShouldCallTarget(result, ifTrue: ExpressionStatement
                            ( 
                                expression: RegisterEvent(@event, TARGET, add: false))
                            )
                        }
                    )
                )
            };
        }

        public static MethodDeclarationSyntax PropertyAccess()
        {
            //
            // private static PropertyInfo PropertyAccess<TResult>(Expression<Func<TResult>> propertyAccess) => (PropertyInfo) ((MemberExpression) propertyAccess.Body).Member;
            //
            
            const string paramName = "propertyAccess";

            Type TResult = typeof(Func<>).GetGenericArguments().Single();  // ugly

            return DeclareMethod
            (
                returnType: typeof(PropertyInfo), 
                name: nameof(PropertyAccess), 
                modifiers: new []{ SyntaxKind.PrivateKeyword, SyntaxKind.StaticKeyword },
                genericArguments: new []{ TResult.Name },
                parameters: new Dictionary<string, Type>
                {
                    {paramName, typeof(Expression<>).MakeGenericType(typeof(Func<>).MakeGenericType(TResult))} // csak egyszer fut le interface-enkent -> nem kell gyorsitotarazni
                }
            )
            .WithExpressionBody
            (
                expressionBody: ArrowExpressionClause
                (
                    expression: CastMemberAccess<PropertyInfo>
                    (
                        expression: ParenthesizedExpression
                        (
                            CastMemberAccess<MemberExpression>
                            (
                                expression: IdentifierName(paramName), 
                                name: nameof(Expression<Action>.Body)
                            )
                        ), 
                        name: nameof(MemberExpression.Member)
                    )
                )
            )
            .WithSemicolonToken
            (
                semicolonToken: Token(SyntaxKind.SemicolonToken)
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

        public static MethodDeclarationSyntax MethodAccess()
        {
            //
            // private static MethodInfo MethodAccess(Expression<Action> methodAccess) => ((MethodCallExpression) methodAccess.Body).Method;
            //
            
            const string paramName = "methodAccess";

            return DeclareMethod
            (
                returnType: typeof(MethodInfo),
                name: nameof(MethodAccess),
                modifiers: new[] { SyntaxKind.PrivateKeyword, SyntaxKind.StaticKeyword },
                genericArguments: new string[0],
                parameters: new Dictionary<string, Type>
                {
                    {paramName, typeof(Expression<>).MakeGenericType(typeof(Action))} // csak egyszer fut le interface-enkent -> nem kell gyorsitotarazni
                }
            )
            .WithExpressionBody
            (
                expressionBody: ArrowExpressionClause
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
                                    expression: IdentifierName(paramName),
                                    name: IdentifierName(nameof(Expression<Action>.Body))
                                )
                            ) 
                        ),
                        name: IdentifierName(nameof(MethodCallExpression.Method))
                    )
                )
            )
            .WithSemicolonToken
            (
                semicolonToken: Token(SyntaxKind.SemicolonToken)
            );
        }

        public static MethodDeclarationSyntax GetEvent()
        {
            //
            // private static EventInfo GetEvent(string eventName) => typeof(TInterface).GetEvent(eventName, BindingFlags.Public | BindingFlags.Instance);
            //

            Type interfaceType = typeof(TInterface);

            Debug.Assert(interfaceType.IsInterface());
            const string paramName = "eventName";

            TypeSyntax bf = CreateType<BindingFlags>();

            return DeclareMethod
            (
                returnType: typeof(EventInfo),
                name: nameof(GetEvent),
                modifiers: new[] { SyntaxKind.PrivateKeyword, SyntaxKind.StaticKeyword },
                genericArguments: new string[0],
                parameters: new Dictionary<string, Type>
                {
                    {paramName, typeof(string)} 
                }
            )
            .WithExpressionBody
            (
                expressionBody: ArrowExpressionClause
                (
                    expression: InvocationExpression
                    (
                        expression: MemberAccess
                        (
                            expression: TypeOfExpression
                            (
                                type: CreateType(interfaceType)
                            ),
                            name: nameof(System.Reflection.TypeInfo.GetEvent)
                        )
                    )
                    .WithArgumentList
                    (
                        argumentList: ArgumentList
                        (
                            arguments: new ExpressionSyntax[]
                            {
                                IdentifierName(paramName),
                                BinaryExpression
                                (
                                    kind: SyntaxKind.BitwiseOrExpression,
                                    left: MemberAccess
                                    (
                                        expression: bf,
                                        name: nameof(BindingFlags.Public)
                                    ),
                                    right: MemberAccess
                                    (
                                        expression: bf,
                                        name: nameof(BindingFlags.Instance)
                                    )
                                )    
                            }
                            .CreateList(Argument)
                        )
                    )
                )
            )
            .WithSemicolonToken
            (
                semicolonToken: Token(SyntaxKind.SemicolonToken)
            );

            MemberAccessExpressionSyntax MemberAccess(ExpressionSyntax expression, string name) => MemberAccessExpression
            (
                kind: SyntaxKind.SimpleMemberAccessExpression,
                expression: expression,
                name: IdentifierName(name)
            );
        }

        public static string AssemblyName => $"{CreateType<TInterceptor>()}_{CreateType<TInterface>()}_Proxy"; 

        public static ClassDeclarationSyntax GenerateProxyClass()
        {
            Type
                interfaceType   = typeof(TInterface),
                interceptorType = typeof(TInterceptor);

            Debug.Assert(interfaceType.IsInterface());

            ClassDeclarationSyntax cls = ClassDeclaration(GeneratedClassName)
            .WithModifiers
            (
                modifiers: TokenList
                (
                    //
                    // Az osztaly ne publikus legyen h "internal" lathatosagu tipusokat is hasznalhassunk
                    //

                    Token(SyntaxKind.InternalKeyword),
                    Token(SyntaxKind.SealedKeyword)
                )
            )
            .WithBaseList
            (
                baseList: BaseList
                (
                    new[] { interceptorType, interfaceType}.CreateList<Type, BaseTypeSyntax>(t => SimpleBaseType(CreateType(t)))
                )
            );

            List<MemberDeclarationSyntax> members = new List<MemberDeclarationSyntax>(new MemberDeclarationSyntax[]
            {
                DeclareCtor(interceptorType.GetApplicableConstructor())
            });

            //
            // BindingFlags.FlattenHierarchy nem mukodik interface-ekre.
            //

            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public;

            MethodInfo[] methods = GetMethods(interfaceType);
            if (methods.Any())
            {
                members.Add(MethodAccess());
                members.AddRange(methods.Select(GenerateProxyMethod));
            }

            PropertyInfo[] properties = GetProperties(interfaceType);
            if (properties.Any())
            {
                members.Add(PropertyAccess());
                members.AddRange(properties.Select(GenerateProxyProperty));
            }

            EventInfo[] events = GetEvents(interfaceType);
            if (events.Any())
            {
                members.Add(GetEvent());
                members.AddRange(events.SelectMany(GenerateProxyEvent));
            }

            return cls.WithMembers(List(members));

            MethodInfo[] GetMethods(Type type) => type
                .GetMethods(bindingFlags)
                .Where(method => !method.IsSpecialName)
                .Concat
                (
                    type.GetInterfaces().SelectMany(GetMethods)
                )
                .Distinct()
                .ToArray();

            PropertyInfo[] GetProperties(Type type) => type
                .GetProperties(bindingFlags)
                .Concat
                (
                    type.GetInterfaces().SelectMany(GetProperties)
                )
                .Distinct()
                .ToArray();

            EventInfo[] GetEvents(Type type) => type
                .GetEvents(bindingFlags)
                .Concat
                (
                    type.GetInterfaces().SelectMany(GetEvents)
                )
                .Distinct()
                .ToArray();
        }
        #endregion
    }
}