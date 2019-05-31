/********************************************************************************
* IStateful.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes an entity that has state.
    /// </summary>
    public interface IStateful
    {
        /// <summary>
        /// Indicates whether the entity is locked or not. The state of locked entities can not be modified.
        /// </summary>
        bool Locked { get; }

        /// <summary>
        /// Locks the current entity.
        /// </summary>
        void Lock();
    }
}