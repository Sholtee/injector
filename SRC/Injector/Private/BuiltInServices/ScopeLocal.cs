/********************************************************************************
* ScopeLocal.cs                                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    using Primitives.Patterns;

    internal sealed class ScopeLocal
    {
        private readonly WriteOnce<object> FValue = new();

        public object Value
        {
            get => FValue.Value!;
            internal set => FValue.Value = value;
        }
    }
}
