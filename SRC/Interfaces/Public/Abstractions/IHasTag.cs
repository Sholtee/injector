/********************************************************************************
* IHasTag.cs                                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Classes implementing this interface may have a tag.
    /// </summary>
    public interface IHasTag
    {
        /// <summary>
        /// User defined custom tag.
        /// </summary>
        object? Tag { get; }
    }
}