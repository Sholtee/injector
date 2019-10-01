/********************************************************************************
* Visibility.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Runtime.CompilerServices;

[
assembly: 
    InternalsVisibleTo("Injector.Tests"), 
    InternalsVisibleTo("Injector.Perf"), 
    InternalsVisibleTo("Solti.Utils.DI.Internals.ParameterValidatorProxy<Solti.Utils.DI.IServiceContainer>_Solti.Utils.DI.IServiceContainer_Proxy"),
    InternalsVisibleTo("Solti.Utils.DI.Internals.ParameterValidatorProxy<Solti.Utils.DI.IInjector>_Solti.Utils.DI.IInjector_Proxy"),
    InternalsVisibleTo("System.Runtime.Loader.AssemblyLoadContext_Solti.Utils.DI.IAssemblyLoadContext_Duck"),
    InternalsVisibleTo("DynamicProxyGenAssembly2") // Moq
]