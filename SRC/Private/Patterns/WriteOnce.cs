/********************************************************************************
* WriteOnce.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    internal sealed class WriteOnce<T>
    {
        private T FValue;

        public WriteOnce(bool strict = true) => Strict = strict;

        public bool Strict { get; }

        public bool HasValue { get; private set; }

        public T Value
        {
            get
            {
                if (!HasValue && Strict) throw new InvalidOperationException();
                return FValue;
            }
            set
            {
                if (HasValue) throw new InvalidOperationException();
                FValue = value;
                HasValue = true;
            }
        }

        public static implicit operator T(WriteOnce<T> value) => value.Value;
    }
}
