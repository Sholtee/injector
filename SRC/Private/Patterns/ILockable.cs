/********************************************************************************
* ILockable.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Describes an entity that can be locked.
    /// </summary>
    public interface ILockable
    {
        /// <summary>
        /// Indicates whether the entity was locked or not. The state of locked entities can not be modified.
        /// </summary>
        bool Locked { get; }

        /// <summary>
        /// Locks the current entity.
        /// </summary>
        void Lock();
    }
}