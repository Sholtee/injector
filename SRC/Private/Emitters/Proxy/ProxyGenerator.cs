/********************************************************************************
* ProxyGenerator.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
                        expression: IdentifierName(nameof(InterfaceInterceptor<TInterface>.MethodAccess))
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
                                            // GetDummyName() azert kell mert ByRef parameterek nem szerepelhetnek kifejezesekben.
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

        internal static StatementSyntax ReadTargetAndReturn(PropertyInfo property) => ReturnStatement(PropertyAccessExpression(property, TARGET));

        internal static StatementSyntax WriteTarget(PropertyInfo property) => ExpressionStatement
        (
            expression: AssignmentExpression
            (
                kind: SyntaxKind.SimpleAssignmentExpression,
                left: PropertyAccessExpression(property, TARGET),
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
            //     if (result == CALL_TARGET) return Target.Foo(para1, ref para2, out para3, para4); // void visszateresnel ures return -> argumentumok semmi kepp sem lesznek visszairva
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

        public static IEnumerable<MemberDeclarationSyntax> GenerateProxyProperty(PropertyInfo ifaceProperty)
        {
            //
            // private static readonly PropertyInfo FProp = Properties["Prop"];
            //
            // TResult IInterface.Prop
            // {
            //     get 
            //     {
            //         object result = Invoke(FProp.GetMethod, new object[0], FProp);
            //         if (result == CALL_TARGET) return Target.Prop;
            //
            //         return (TResult) result;
            //     }
            //     set
            //     {
            //         object result = Invoke(FProp.SetMethod, new object[]{ value }, FProp);
            //         if (result == CALL_TARGET) Target.Prop = value;
            //     }
            // }
            //

            IdentifierNameSyntax fieldName = IdentifierName($"F{ifaceProperty.Name}");

            yield return DeclareField<PropertyInfo>
            (
                name: fieldName.Identifier.Text,
                initializer: ElementAccessExpression
                (
                    expression: IdentifierName(nameof(InterfaceInterceptor<TInterface>.Properties))
                )
                .WithArgumentList
                (
                    argumentList: BracketedArgumentList
                    (
                        arguments: SingletonSeparatedList
                        (
                            Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(ifaceProperty.Name)))
                        )
                    )
                ),
                modifiers: new[]
                {
                    SyntaxKind.PrivateKeyword,
                    SyntaxKind.StaticKeyword,
                    SyntaxKind.ReadOnlyKeyword
                }
            );

            if (ifaceProperty.IsIndexer())
            {
                yield return GenerateProxyIndexer(ifaceProperty);
                yield break;
            }

            LocalDeclarationStatementSyntax result;

            //
            // Nem gond ha mondjuk az interface property-nek nincs gettere, akkor a "getBody"
            // figyelmen kivul lesz hagyva.
            //

            yield return DeclareProperty
            (
                property: ifaceProperty,
                getBody: Block
                (
                    statements: new StatementSyntax[]
                    {
                        result = CallInvoke
                        (
                            MemberAccessExpression // FProp.GetMethod
                            (
                                kind: SyntaxKind.SimpleMemberAccessExpression,
                                expression: fieldName,
                                name: IdentifierName(nameof(PropertyInfo.GetMethod))
                            ),
                            CreateArray<object>(), // new object[0]
                            fieldName // FProp
                        ),
                        ShouldCallTarget(result, ifTrue: ReadTargetAndReturn(ifaceProperty)),
                        ReturnResult(ifaceProperty.PropertyType, result)
                    }
                ),
                setBody: Block
                (
                    statements: new StatementSyntax[]
                    {
                        result = CallInvoke
                        (
                            MemberAccessExpression // FProp.SetMethod
                            (
                                kind: SyntaxKind.SimpleMemberAccessExpression,
                                expression: fieldName,
                                name: IdentifierName(nameof(PropertyInfo.SetMethod))
                            ),
                            CreateArray<object>(IdentifierName(Value)), // new object[] {value}
                            fieldName // FProp
                        ),
                        ShouldCallTarget(result, ifTrue: WriteTarget(ifaceProperty))
                    }
                )
            );
        }

        public static MemberDeclarationSyntax GenerateProxyIndexer(PropertyInfo ifaceProperty)
        {
            //
            // TResult IInterface.this[TParam1 p1, TPAram2 p2]
            // {
            //     get 
            //     {
            //         object result = Invoke(FProp.GetMethod, new object[]{ p1, p2 }, FProp);
            //         if (result == CALL_TARGET) return Target[p1, p2];
            //
            //         return (TResult) result;
            //     }
            //     set
            //     {
            //         object result = Invoke(FProp.SetMethod, new object[]{ p1, p2, value }, FProp);
            //         if (result == CALL_TARGET) Target[p1, p2] = value;
            //     }
            // }
            //

            IdentifierNameSyntax fieldName = IdentifierName($"F{ifaceProperty.Name}");

            LocalDeclarationStatementSyntax result;

            return DeclareIndexer
            (
                property: ifaceProperty,
                getBody: paramz => Block
                (
                    statements: new StatementSyntax[]
                    {
                        result = CallInvoke
                        (
                            MemberAccessExpression // FProp.GetMethod
                            (
                                kind: SyntaxKind.SimpleMemberAccessExpression,
                                expression: fieldName,
                                name: IdentifierName(nameof(PropertyInfo.GetMethod))
                            ),
                            CreateArray<object>(paramz // new object[] {p1, p2}
                                .Select(param => IdentifierName(param.Identifier))
                                .Cast<ExpressionSyntax>()
                                .ToArray()),
                            fieldName // FProp
                        ),
                        ShouldCallTarget(result, ifTrue: ReadTargetAndReturn(ifaceProperty)),
                        ReturnResult(ifaceProperty.PropertyType, result)
                    }
                ),
                setBody: paramz => Block
                (
                    statements: new StatementSyntax[]
                    {
                        result = CallInvoke
                        (
                            MemberAccessExpression // FProp.SetMethod
                            (
                                kind: SyntaxKind.SimpleMemberAccessExpression,
                                expression: fieldName,
                                name: IdentifierName(nameof(PropertyInfo.SetMethod))
                            ),
                            CreateArray<object>(paramz // new object[] {p1, p2, value}
                                .Select(param => IdentifierName(param.Identifier))
                                .Append(IdentifierName(Value))
                                .Cast<ExpressionSyntax>()
                                .ToArray()),
                            fieldName // FProp
                        ),
                        ShouldCallTarget(result, ifTrue: WriteTarget(ifaceProperty))
                    }
                )
            );
        }

        public static IEnumerable<MemberDeclarationSyntax> GenerateProxyEvent(EventInfo @event)
        {
            //
            // private static readonly EventInfo FEvent = Events("Event");
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

            IdentifierNameSyntax fieldName = IdentifierName($"F{@event.Name}");

            LocalDeclarationStatementSyntax result;

            yield return  DeclareField<EventInfo>
            (
                name: fieldName.Identifier.Text, 
                initializer: ElementAccessExpression
                (
                    expression: IdentifierName(nameof(InterfaceInterceptor<TInterface>.Events))
                )
                .WithArgumentList
                (
                    argumentList: BracketedArgumentList
                    (
                        arguments: SingletonSeparatedList
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
            );

            yield return DeclareEvent
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
                                expression: fieldName,
                                name: IdentifierName(nameof(EventInfo.AddMethod))
                            ),
                            CreateArray<object>(IdentifierName(Value)), // new object[] {value}
                            fieldName //FEvent
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
                                expression: fieldName,
                                name: IdentifierName(nameof(EventInfo.RemoveMethod))
                            ),
                            CreateArray<object>(IdentifierName(Value)),
                            fieldName
                        ),
                        ShouldCallTarget(result, ifTrue: ExpressionStatement
                        ( 
                            expression: RegisterEvent(@event, TARGET, add: false))
                        )
                    }
                )
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

            members.AddRange
            (
                interfaceType
                    .ListInterfaceMembers(System.Reflection.TypeExtensions.GetMethods)
                    .Where(m => !m.IsSpecialName)
                    .Select(GenerateProxyMethod)
            );

            members.AddRange
            (
                interfaceType
                    .ListInterfaceMembers(System.Reflection.TypeExtensions.GetProperties)
                    .SelectMany(GenerateProxyProperty)
            );

            members.AddRange
            (
                interfaceType
                    .ListInterfaceMembers(System.Reflection.TypeExtensions.GetEvents)
                    .SelectMany(GenerateProxyEvent)
            );

            return cls.WithMembers(List(members));
        }
        #endregion
    }
}