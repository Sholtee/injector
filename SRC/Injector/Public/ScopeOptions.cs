/********************************************************************************
* ScopeOptions.cs                                                               *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Runtime.CompilerServices;
using Solti.Utils.DI.Interfaces;

#pragma warning disable RS0016  // workaround for bug in Microsoft.CodeAnalysis.PublicApiAnalyzers when trying to forward record types
[assembly: TypeForwardedTo(typeof(ScopeOptions))]