/********************************************************************************
* Consts.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Diagnostics.CodeAnalysis;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Contains some constants related to this library.
    /// </summary>
    public static class Consts
    {
        /// <summary>
        /// Marks a service as internal
        /// </summary>
        [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Using upper case and underscore together highlights that the member is a constant value")]
        public const string INTERNAL_SERVICE_NAME_PREFIX = "$";
    }
}
