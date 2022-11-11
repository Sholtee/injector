/********************************************************************************
* ServiceResolverExtensions.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal static class ServiceResolverExtensions
    {
        public static AbstractServiceEntry GetUnderlyingEntry(this ServiceResolver resolver)
        {
            object target = resolver.Target;

            if (target is AbstractServiceEntry entry)
                return entry;

            foreach (FieldInfo fld in target.GetType().GetFields())
            {
                if (fld.FieldType.IsSubclassOf(typeof(AbstractServiceEntry)))
                    return (AbstractServiceEntry) fld.GetValue(target);
            }

            throw new InvalidOperationException();
        }
    }
}
