﻿/********************************************************************************
* ProxyGeneratorBase.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.DI.Internals
{
    internal static class ProxyGeneratorBase
    {
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

        public static MethodDeclarationSyntax DeclareMethod(
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

        public static MethodDeclarationSyntax DeclareMethod(MethodInfo method)
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

        public static PropertyDeclarationSyntax DeclareProperty(PropertyInfo property, BlockSyntax getBody = null, BlockSyntax setBody = null)
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

        public static EventDeclarationSyntax DeclareEvent(EventInfo @event, BlockSyntax addBody = null, BlockSyntax removeBody = null)
        {
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

            if (@event.AddMethod    != null && addBody    != null) accessors.Add(AccessorDeclaration(SyntaxKind.AddAccessorDeclaration).WithBody(addBody));
            if (@event.RemoveMethod != null && removeBody != null) accessors.Add(AccessorDeclaration(SyntaxKind.RemoveAccessorDeclaration).WithBody(removeBody));

            return !accessors.Any() ? result : result.WithAccessorList
            (
                accessorList: AccessorList
                (
                    accessors: List(accessors)
                )
            );
        }

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
                            .ToArray() // ToArray() kell =(
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

        public static SeparatedSyntaxList<TNode> CreateList<T, TNode>(this IReadOnlyCollection<T> src, Func<T, TNode> factory) where TNode : SyntaxNode => SeparatedList<TNode>
        (
            nodesAndTokens: src.SelectMany((p, i) =>
            {
                var l = new List<SyntaxNodeOrToken> { factory(p) };
                if (i < src.Count - 1) l.Add(Token(SyntaxKind.CommaToken));

                return l;
            })
        );

        public static NameSyntax GetQualifiedName(this Type type, Func<string, NameSyntax> typeNameFactory = null)
        {
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
    }
}
