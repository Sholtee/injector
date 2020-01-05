/********************************************************************************
* Visibility.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Solti.Utils.DI.Internals
{
    using Properties;
    using static ProxyGeneratorBase;

    internal static class Visibility
    {
        private enum AccessModifier
        {
            Private,
            Protected,
            Internal,
            ProtectedInternal,
            Public
        }

        //
        // Hasznalhato Property-khez is: 
        //   Property.GetMethod?.GetAccessModifier(), PRoperty.SetMethod?.GetAccessModifier()
        //

        private static AccessModifier GetAccessModifier(MethodInfo method)
        {
            if (method.IsPrivate)          return AccessModifier.Private;
            if (method.IsFamily)           return AccessModifier.Protected;
            if (method.IsAssembly)         return AccessModifier.Internal;
            if (method.IsFamilyOrAssembly) return AccessModifier.ProtectedInternal;
            if (method.IsPublic)           return AccessModifier.Public;

            throw new Exception(Resources.UNDETERMINED_ACCESS_MODIFIER);
        }

        public static void Check(MethodInfo method, string assemblyName) 
        {
            AccessModifier am = GetAccessModifier(method);

            switch (am) 
            {
                case AccessModifier.Internal:
                case AccessModifier.ProtectedInternal:
                    bool grantedByAttr = method
                        .DeclaringType
                        .Assembly()
                        .GetCustomAttributes<InternalsVisibleToAttribute>()
                        .FirstOrDefault(ivt => ivt.AssemblyName == assemblyName) != null;
                    if (!grantedByAttr)
                        throw new Exception(string.Format(Resources.Culture, Resources.IVT_REQUIRED, method.GetFullName(), assemblyName));
                    break;
                case AccessModifier.Protected:
                case AccessModifier.Private:
                    throw new Exception(string.Format(Resources.Culture, Resources.METHOD_NOT_VISIBLE, method.GetFullName()));
            }

            Debug.Assert(am == AccessModifier.Public, "Unknown AccessModifier");
        }

        public static void Check(PropertyInfo property, string assemblyName, bool checkGet = true, bool checkSet = true) 
        {
            if (checkGet) 
            {
                MethodInfo get = property.GetMethod;
                Debug.Assert(get != null, "property.GetMethod == NULL");

                Check(get, assemblyName);
            }

            if (checkSet)
            {
                MethodInfo set = property.SetMethod;
                Debug.Assert(set != null, "property.SetMethod == NULL");

                Check(set, assemblyName);
            }
        }

        public static void Check(EventInfo @event, string assemblyName, bool checkAdd = true, bool checkRemove = true) 
        {
            if (checkAdd)
            {
                MethodInfo add = @event.AddMethod;
                Debug.Assert(add != null, "event.AddMethod == NULL");

                Check(add, assemblyName);
            }

            if (checkRemove)
            {
                MethodInfo remove = @event.RemoveMethod;
                Debug.Assert(remove != null, "event.RemoveMethod == NULL");

                Check(remove, assemblyName);
            }
        }

        private static bool GrantedFor(CompilationUnitSyntax unitToCheck, string asmName, params Assembly[] references)
        {
            //
            // Mivel az "internal" es "protected" kulcsszavak nem leteznek IL szinten ezert reflexioval
            // nem tudnank megallapitani h a tipus lathato e a kodunk szamara szoval a forditora bizzuk
            // a dontest.
            //

            Debug.WriteLine(unitToCheck.NormalizeWhitespace().ToFullString());

            CSharpCompilation compilation = CSharpCompilation.Create
            (
                assemblyName: asmName,
                syntaxTrees: new[]
                {
                    CSharpSyntaxTree.Create
                    (
                        root: unitToCheck
                    )
                },
                references: Runtime
                    .Assemblies
                    .Concat(references)
                    .Distinct()
                    .Select(asm => MetadataReference.CreateFromFile(asm.Location)),
                options: CompilationOptionsFactory.Create()
            );

            Diagnostic[] diagnostics = compilation
                .GetDeclarationDiagnostics()
                .Where(diag => diag.Severity == DiagnosticSeverity.Error)
                .ToArray();

            Debug.Assert(diagnostics.Length <= 1, "Too many errors");

            //
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs0122
            //

            return !diagnostics.Any(diag => diag.Id.Equals("CS0122", StringComparison.OrdinalIgnoreCase));
        }

        public static bool GrantedFor(Type type, string asmName) // TODO: ez dobja a kivetelt
        {
            //
            // using t = Namespace.Type;
            //

            CompilationUnitSyntax unitToCheck = CompilationUnit().WithUsings
            (
                usings: SingletonList
                (
                    UsingDirective
                    (
                        name: (NameSyntax) CreateType(type)
                    )
                    .WithAlias
                    (
                        alias: NameEquals(IdentifierName("t"))
                    )
                )
            );

            return GrantedFor(unitToCheck, asmName, type.GetReferences().ToArray());
        }

        public static bool GrantedFor(MemberInfo member, string asmName)  // TODO: ez dobja a kivetelt
        {
            //
            // public class Cls 
            // {
            //    private void Foo() {}
            //    public void Foo(int i) {}
            // }
            //
            // "nameof(Cls.Foo)" fordulni fog meg ha mi a parameter nelkuli metodust is vizsgalnank.
            //
            // Megjegyzendo h "Foo"-val mar nem lehet property szoval azzal nem lenne gond.
            //

            throw new NotImplementedException();
        }
    }
}