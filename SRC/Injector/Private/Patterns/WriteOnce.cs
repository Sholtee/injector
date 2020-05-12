/********************************************************************************
* WriteOnce.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal sealed class WriteOnce
    {
        private object? FValue;

        public WriteOnce(bool strict = true) => Strict = strict;

        public bool Strict { get; }

        public bool HasValue { get; private set; }

        public object? Value
        {
            get
            {
                if (!HasValue && Strict) throw new InvalidOperationException(); // TODO
                return FValue;
            }
            set
            {
                if (HasValue) throw new InvalidOperationException(Resources.VALUE_ALREADY_SET);
                FValue = value;
                HasValue = true;
            }
        }        
    }
}
