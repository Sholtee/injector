/********************************************************************************
* IRoslynCompilable.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Solti.Utils.DI.Internals
{
    internal interface IRoslynCompilable
    {
        /// <summary>
        /// Builds the factory to be called with the given <paramref name="closures"/>
        /// </summary>
        /// <remarks>The declared method must have the layout of <code>public static object? Create_XxX(object? closures){...}</code></remarks>
        MethodDeclarationSyntax BuildFactory(out object? closures);
    }
}
