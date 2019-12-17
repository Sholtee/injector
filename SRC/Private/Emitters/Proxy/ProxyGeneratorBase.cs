﻿/********************************************************************************
* ProxyGeneratorBase.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.DI.Internals
{
    internal static class ProxyGeneratorBase
    {
        public const string
            // https://github.com/dotnet/roslyn/issues/4861
            Value = "value",
            GeneratedClassName = "GeneratedProxy";


        private static SyntaxList<AttributeListSyntax> DeclareMethodImplAttributeToForceInlining() => SingletonList
        (
            node: AttributeList
            (
                attributes: SingletonSeparatedList
                (
                    node: Attribute
                    (
                        (NameSyntax)CreateType<MethodImplAttribute>()
                    )
                    .WithArgumentList
                    (
                        argumentList: AttributeArgumentList
                        (
                            arguments: SingletonSeparatedList
                            (
                                node: AttributeArgument
                                (
                                    expression: MemberAccessExpression
                                    (
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        CreateType<MethodImplOptions>(),
                                        IdentifierName(nameof(MethodImplOptions.AggressiveInlining))
                                    )
                                )
                            )
                        )
                    )
                )
            )
        );

        private static AccessorDeclarationSyntax DeclareAccessor(SyntaxKind kind, CSharpSyntaxNode body, bool forceInlining)
        {
            AccessorDeclarationSyntax declaration = AccessorDeclaration(kind);

            switch (body)
            {
                case BlockSyntax block:
                    declaration = declaration.WithBody(block);
                    break;
                case ArrowExpressionClauseSyntax arrow:
                    declaration = declaration
                        .WithExpressionBody(arrow)
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
                    break;
                default:
                    Debug.Fail("Unknown node type");
                    return null;
            }

            if (forceInlining) declaration = declaration.WithAttributeLists
            (
                attributeLists: DeclareMethodImplAttributeToForceInlining()
            );

            return declaration;
        }

        public static LocalDeclarationStatementSyntax DeclareLocal(Type type, string name, ExpressionSyntax initializer = null)
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

        public static LocalDeclarationStatementSyntax DeclareLocal<T>(string name, ExpressionSyntax initializer = null) => DeclareLocal(typeof(T), name, initializer);

        public static ArrayCreationExpressionSyntax CreateArray<T>(params ExpressionSyntax[] elements) => ArrayCreationExpression
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

        public static MethodDeclarationSyntax DeclareMethod(MethodInfo method, bool forceInlining = false)
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

                        List<SyntaxKind> modifiers = new List<SyntaxKind>();

                        if (param.IsOut) 
                            modifiers.Add(SyntaxKind.OutKeyword);

                        else if (param.IsIn) 
                            modifiers.Add(SyntaxKind.InKeyword);

                        //
                        // "ParameterType.IsByRef" param.Is[In|Out] eseten is igazat ad vissza -> a lenti feltetel In|Out vizsgalat utan szerepeljen.
                        //

                        else if (param.ParameterType.IsByRef)
                            modifiers.Add(SyntaxKind.RefKeyword);

                        //
                        // "params" es referencia szerinti parameter atadas egymast kizaroak
                        //

                        else if (param.GetCustomAttribute<ParamArrayAttribute>() != null) 
                            modifiers.Add(SyntaxKind.ParamsKeyword);

                        if (modifiers.Any()) 
                            parameter = parameter.WithModifiers(TokenList(modifiers.Select(Token)));
     
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

            if (forceInlining) result = result.WithAttributeLists
            (
                attributeLists: DeclareMethodImplAttributeToForceInlining() 

            );

            return result;
        }

        public static PropertyDeclarationSyntax DeclareProperty(PropertyInfo property, CSharpSyntaxNode getBody = null, CSharpSyntaxNode setBody = null, bool forceInlining = false)
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

            if (property.CanRead && getBody != null)
                accessors.Add(DeclareAccessor(SyntaxKind.GetAccessorDeclaration, getBody, forceInlining));

            if (property.CanWrite && setBody != null)
                accessors.Add(DeclareAccessor(SyntaxKind.SetAccessorDeclaration, setBody, forceInlining));

            return !accessors.Any() ? result : result.WithAccessorList
            (
                accessorList: AccessorList
                (
                    accessors: List(accessors)
                )
            );
        }

        public static IndexerDeclarationSyntax DeclareIndexer(PropertyInfo property, Func<IReadOnlyList<ParameterSyntax>, CSharpSyntaxNode> getBody = null, Func<IReadOnlyList<ParameterSyntax>, CSharpSyntaxNode> setBody = null, bool forceInlining = false)
        {
            Debug.Assert(property.DeclaringType.IsInterface());
            Debug.Assert(property.IsIndexer());

            ParameterInfo[] indices = property.GetIndexParameters();

            IndexerDeclarationSyntax result = IndexerDeclaration
            (
                type: CreateType(property.PropertyType)
            )
            .WithExplicitInterfaceSpecifier
            (
                explicitInterfaceSpecifier: ExplicitInterfaceSpecifier((NameSyntax) CreateType(property.DeclaringType))
            )
            .WithParameterList
            (
                parameterList: BracketedParameterList
                (
                    parameters: indices.CreateList
                    (
                        index => Parameter
                        (
                            identifier: Identifier(index.Name)                          
                        )
                        .WithType
                        (
                            type: CreateType(index.ParameterType)
                        )
                    )
                )
            );

            List<AccessorDeclarationSyntax> accessors = new List<AccessorDeclarationSyntax>();

            if (property.CanRead && getBody != null)
                accessors.Add(DeclareAccessor(SyntaxKind.GetAccessorDeclaration, getBody(result.ParameterList.Parameters), forceInlining));

            if (property.CanWrite && setBody != null)
                accessors.Add(DeclareAccessor(SyntaxKind.SetAccessorDeclaration, setBody(result.ParameterList.Parameters), forceInlining));

            return !accessors.Any() ? result : result.WithAccessorList
            (
                accessorList: AccessorList
                (
                    accessors: List(accessors)
                )
            );
        }

        public static FieldDeclarationSyntax DeclareField<TField>(string name, ExpressionSyntax initializer = null, params SyntaxKind[] modifiers)
        {
            VariableDeclaratorSyntax declarator = VariableDeclarator
            (
                identifier: Identifier(name)
            );

            if (initializer != null) declarator = declarator.WithInitializer
            (
                initializer: EqualsValueClause(initializer)
            );

            return FieldDeclaration
            (
                VariableDeclaration
                (
                    type: CreateType<TField>()                    
                )
                .WithVariables
                (
                    variables: SingletonSeparatedList(declarator)
                )
            )
            .WithModifiers
            (
                modifiers: TokenList
                (
                    tokens: modifiers.Select(Token)
                )
            );
        }

        public static EventDeclarationSyntax DeclareEvent(EventInfo @event, CSharpSyntaxNode addBody = null, CSharpSyntaxNode removeBody = null, bool forceInlining = false)
        {
            Debug.Assert(@event.DeclaringType.IsInterface());

            EventDeclarationSyntax result = EventDeclaration
            (
                type: CreateType(@event.EventHandlerType),
                identifier: Identifier(@event.Name)
            )
            .WithExplicitInterfaceSpecifier
            (
                explicitInterfaceSpecifier: ExplicitInterfaceSpecifier((NameSyntax) CreateType(@event.DeclaringType))
            );

            List<AccessorDeclarationSyntax> accessors = new List<AccessorDeclarationSyntax>();

            if (@event.AddMethod != null && addBody != null)
                accessors.Add(DeclareAccessor(SyntaxKind.AddAccessorDeclaration, addBody, forceInlining));

            if (@event.RemoveMethod != null && removeBody != null)
                accessors.Add(DeclareAccessor(SyntaxKind.RemoveAccessorDeclaration, removeBody, forceInlining));

            return !accessors.Any() ? result : result.WithAccessorList
            (
                accessorList: AccessorList
                (
                    accessors: List(accessors)
                )
            );
        }

        public static AssignmentExpressionSyntax RegisterEvent(EventInfo @event, string target, bool add) => AssignmentExpression
        (
            kind: add ? SyntaxKind.AddAssignmentExpression : SyntaxKind.SubtractAssignmentExpression,
            left: MemberAccessExpression
            (
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName(target),
                IdentifierName(@event.Name)
            ),
            right: IdentifierName(Value)
        );

        public static ExpressionSyntax PropertyAccessExpression(PropertyInfo property, string target) => property.IsIndexer()
            ? ElementAccessExpression
            (
                expression: IdentifierName(target),
                argumentList: BracketedArgumentList
                (
                    arguments: property
                        .GetIndexParameters()
                        .CreateList(param => Argument(IdentifierName(param.Name)))
                )
            )
            : (ExpressionSyntax) MemberAccessExpression
            (
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName(target),
                IdentifierName(property.Name)
            );

        public static TypeSyntax CreateType(Type src)
        {
            //
            // "Cica<T>.Mica<TT>"-nal a "TT" is beagyazott ami nekunk nem jo
            //

            if (src.IsNested && !src.IsGenericParameter)
            {
                IEnumerable<NameSyntax> partNames;

                IEnumerable<Type> parts = src.GetParents();

                if (!src.IsGenericType()) partNames = parts.Append(src).Select(type => type.GetQualifiedName());
                else
                { 
                    //
                    // "Cica<T>.Mica<TT>.Kutya" is generikusnak minosul: Generikus formaban Cica<T>.Mica<TT>.Kutya<T, TT>
                    // mig tipizaltan "Cica<T>.Mica<T>.Kutya<TConcrete1, TConcrete2>".
                    // Ami azert lassuk be igy eleg szopas.
                    //

                    IReadOnlyList<Type> genericArguments = src.GetGenericArguments(); // "<T, TT>" vagy "<TConcrete1, TConcrete2>"

                    partNames = parts.Append(src.GetGenericTypeDefinition()).Select(type =>
                    {
                        int relatedGACount = type.GetOwnGenericArguments().Count;

                        //
                        // Beagyazott tipusnal a GetQualifiedName() a rovid nevet fogja feldolgozni: 
                        // "Cica<T>.Mica<TT>.Kutya<T, TT>" -> "Kutya".
                        //

                        if (relatedGACount > 0)
                        {
                            IEnumerable<Type> relatedGAs = genericArguments.Take(relatedGACount);
                            genericArguments = genericArguments.Skip(relatedGACount).ToArray();

                            return type.GetQualifiedName(name => CreateGenericName(name, relatedGAs.ToArray()));
                        }

                        return type.GetQualifiedName();
                    });
                }

                return Qualify(partNames.ToArray());
            }

            if (src.IsGenericType()) return src.GetGenericTypeDefinition().GetQualifiedName
            (
                name => CreateGenericName(name, src.GetGenericArguments())
            );

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

                        sizes: Enumerable
                            .Repeat(0, src.GetArrayRank())
                            .CreateList(@void => (ExpressionSyntax) OmittedArraySizeExpression())
                    )
                )
            );

            return src.GetQualifiedName();

            NameSyntax CreateGenericName(string name, IReadOnlyCollection<Type> genericArguments) => GenericName(name).WithTypeArgumentList
            (
                typeArgumentList: TypeArgumentList
                (
                    arguments: genericArguments.CreateList(CreateType)
                )
            );
        }

        public static TypeSyntax CreateType<T>() => CreateType(typeof(T));

        public static SeparatedSyntaxList<TNode> CreateList<T, TNode>(this IEnumerable<T> src, Func<T, int, TNode> factory) where TNode : SyntaxNode
        {
            int count = (src as IReadOnlyCollection<T>)?.Count ?? (src as ICollection<T>)?.Count ?? (src = src.ToArray()).Count();

            return SeparatedList<TNode>
            (
                nodesAndTokens: src.SelectMany((p, i) =>
                {
                    var l = new List<SyntaxNodeOrToken> {factory(p, i)};
                    if (i < count - 1) l.Add(Token(SyntaxKind.CommaToken));

                    return l;
                })
            );
        }

        public static SeparatedSyntaxList<TNode> CreateList<T, TNode>(this IEnumerable<T> src, Func<T, TNode> factory) where TNode : SyntaxNode => src.CreateList((p, i) => factory(p));

        public static NameSyntax GetQualifiedName(this Type type, Func<string, NameSyntax> typeNameFactory = null)
        {
            Debug.Assert(!type.IsGenericType() || type.IsGenericTypeDefinition());

            return Parts2QualifiedName
            (
                parts: type.GetFriendlyName().Split('.').ToArray(),
                factory: typeNameFactory ?? IdentifierName
            );

            NameSyntax Parts2QualifiedName(IReadOnlyCollection<string> parts, Func<string, NameSyntax> factory) => Qualify
            (
                parts
                    //
                    // Nevter, szulo osztaly (beagyazott tipus eseten)
                    //

                    .Take(parts.Count - 1)
                    .Select(part => (NameSyntax) IdentifierName(part))

                    //
                    // Tipus neve
                    //

                    .Append(factory(parts.Last()))
                    .ToArray()
            );
        }

        public static NameSyntax Qualify(params NameSyntax[] parts) => parts.Length <= 1 ? parts.Single() : QualifiedName
        (
            left: Qualify(parts.Take(parts.Length - 1).ToArray()),
            right: (SimpleNameSyntax) parts.Last()
        );

        public static IdentifierNameSyntax ToIdentifierName(this LocalDeclarationStatementSyntax variable) => IdentifierName(variable.Declaration.Variables.Single().Identifier);

        public static InvocationExpressionSyntax InvokeMethod(MethodInfo method, string target, IReadOnlyList<string> arguments) => InvocationExpression
        (
            expression: MemberAccessExpression
            (
                kind: SyntaxKind.SimpleMemberAccessExpression,
                expression: IdentifierName(target),
                name: IdentifierName(method.Name)
            )
        )
        .WithArgumentList
        (
            argumentList: ArgumentList(method.GetParameters().CreateList((param, i) =>
            {
                ArgumentSyntax argument = Argument
                (
                    expression: IdentifierName(arguments[i])
                );

                if (param.IsOut) argument = argument.WithRefKindKeyword
                (
                    refKindKeyword: Token(SyntaxKind.OutKeyword)
                );

                else if (param.IsIn) argument = argument.WithRefKindKeyword
                (
                    refKindKeyword: Token(SyntaxKind.InKeyword)
                );

                //
                // "ParameterType.IsByRef" param.Is[In|Out] eseten is igazat ad vissza -> a lenti feltetel utoljara szerepeljen.
                //

                else if (param.ParameterType.IsByRef) argument = argument.WithRefKindKeyword
                (
                    refKindKeyword: Token(SyntaxKind.RefKeyword)
                );

                return argument;
            }))
        );

        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "'GeneratedClassName' won't be localized.")]
        public static ConstructorDeclarationSyntax DeclareCtor(ConstructorInfo ctor)
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

        public static CompilationUnitSyntax GenerateProxyUnit
        (
            ClassDeclarationSyntax @class
#if IGNORE_VISIBILITY
            ,
            params string[] ignoresAccessChecksTo
#endif
        )
        {
            CompilationUnitSyntax unit = CompilationUnit().WithMembers
            (
                members: SingletonList<MemberDeclarationSyntax>
                (
                    @class
                )
            );
#if IGNORE_VISIBILITY
            if (ignoresAccessChecksTo.Any()) unit = unit.WithAttributeLists
            (
                SingletonList
                (
                    AttributeList
                    (
                        attributes: ignoresAccessChecksTo.CreateList(CreateIgnoresAccessChecksToAttribute)
                    )
                    .WithTarget
                    (
                        AttributeTargetSpecifier(Token(SyntaxKind.AssemblyKeyword))
                    )
                )
            );
#endif
            return unit;
#if IGNORE_VISIBILITY
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
#endif
        }
    }
}