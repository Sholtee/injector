/********************************************************************************
* Node.cs                                                                       *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    internal sealed class Node<TKey, TValue>
    {
        public Node(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public readonly TKey Key;

        public readonly TValue Value;

        //
        // Intentionally not a propery (to make referencing possible)
        //

        public Node<TKey, TValue>? Next;
    }
}
