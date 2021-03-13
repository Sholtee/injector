/********************************************************************************
* Assembly.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Resources;
using System.Runtime.CompilerServices;

[assembly: NeutralResourcesLanguage("en"), CLSCompliant(true), InternalsVisibleTo("Solti.Utils.DI"),
#if DEBUG
    InternalsVisibleTo("Solti.Utils.DI.Tests")
#endif
]
