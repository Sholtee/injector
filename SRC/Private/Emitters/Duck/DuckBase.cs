/********************************************************************************
* DuckBase.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    /// <summary>
    /// Defines the base class for duck typing.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    public abstract class DuckBase<T>
    {
        /// <summary>
        /// The target.
        /// </summary>
        public T Target { get; }

        public DuckBase(T target) => Target = target; // ne Protected legyen
    }
}
