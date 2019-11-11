/********************************************************************************
* DisposableGenerator.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


namespace Solti.Utils.DI.Internals
{
    using static ProxyGeneratorBase;

    internal static class DisposableGenerator<TInterface>
    {
        private static MethodInfo MethodAccess<T>(Expression<Action<T>> methodAccess) => ((MethodCallExpression) methodAccess.Body).Method;

        public static ClassDeclarationSyntax GenerateDuckClass()
        {
            Type
                interfaceType = typeof(TInterface),
                @base = typeof(DisposableWrapper<TInterface>);

            Debug.Assert(interfaceType.IsInterface());

            ClassDeclarationSyntax cls = ClassDeclaration
            (
                identifier: GeneratedClassName
            )
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
                    new[] { @base, interfaceType }.CreateList<Type, BaseTypeSyntax>(t => SimpleBaseType(CreateType(t)))
                )
            );

            List<MemberDeclarationSyntax> members = new List<MemberDeclarationSyntax>(new MemberDeclarationSyntax[]
            {
                DeclareCtor(@base.GetApplicableConstructor())
            });

            //
            // Az os osztaly megvalositja.
            //

            MethodInfo dispose = MethodAccess<IDisposable>(d => d.Dispose());

            members.AddRange
            (
                interfaceType
                    .ListInterfaceMembers(System.Reflection.TypeExtensions.GetMethods)
                    .Where(m => !m.IsSpecialName && m != dispose)
                    .Select(DuckGenerator<TInterface, TInterface>.GenerateDuckMethod)
            );

            members.AddRange
            (
                interfaceType
                    .ListInterfaceMembers(System.Reflection.TypeExtensions.GetProperties)
                    .Select(DuckGenerator<TInterface, TInterface>.GenerateDuckProperty)
            );

            members.AddRange
            (
                interfaceType
                    .ListInterfaceMembers(System.Reflection.TypeExtensions.GetEvents)
                    .Select(DuckGenerator<TInterface, TInterface>.GenerateDuckEvent)
            );

            return cls.WithMembers(List(members));
        }
    }
}
