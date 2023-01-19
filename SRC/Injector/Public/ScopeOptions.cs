/********************************************************************************
* ScopeOptions.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
#pragma warning disable RS0016  // workaround for bug in Microsoft.CodeAnalysis.PublicApiAnalyzers when trying to forward record types
using System.Runtime.CompilerServices;

using Solti.Utils.DI.Interfaces;

[assembly: TypeForwardedTo(typeof(ScopeOptions))]