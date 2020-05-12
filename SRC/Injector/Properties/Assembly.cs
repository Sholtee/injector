/********************************************************************************
* Visibility.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Resources;
using System.Runtime.CompilerServices;

[
assembly:
    NeutralResourcesLanguage("en"),
#if DEBUG
    InternalsVisibleTo("Injector.Tests"),
    InternalsVisibleTo("DynamicProxyGenAssembly2"), // Moq
#endif
    InternalsVisibleTo("Injector.Perf")
]