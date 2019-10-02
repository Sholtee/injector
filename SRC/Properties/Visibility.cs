/********************************************************************************
* Visibility.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Runtime.CompilerServices;

[
assembly:
#if DEBUG
    InternalsVisibleTo("Injector.Tests"),
    InternalsVisibleTo("DynamicProxyGenAssembly2"), // Moq
#endif
    InternalsVisibleTo("Injector.Perf"),
    InternalsVisibleTo("System.Runtime.Loader.AssemblyLoadContext_Solti.Utils.DI.IAssemblyLoadContext_Duck")    
]