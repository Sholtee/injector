/********************************************************************************
* RelatedEntryKindAttribute.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal sealed class RelatedEntryKindAttribute: Attribute
    {
        public RelatedEntryKindAttribute(Type serviceEntry)
        {
            Debug.Assert(typeof(AbstractServiceEntry).IsAssignableFrom(serviceEntry));
            ServiceEntry = serviceEntry;
        }

        public Type ServiceEntry { get; }
    }
}
