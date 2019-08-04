/********************************************************************************
* ServiceEntryExtensions.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    internal static class ServiceEntryExtensions
    {
        public static void CheckValid(this ServiceEntry entry)
        {
            Type
                @interface     = entry.Interface,
                implementation = entry.Implementation;

            if (!@interface.IsInterfaceOf(implementation))
                throw new InvalidOperationException(string.Format(Resources.NOT_ASSIGNABLE, @interface, implementation));

            implementation.GetApplicableConstructor(); // validal is (mukodik generikus tipusra is)
        }

        public static ServiceEntry Specialize(this ServiceEntry entry, params Type[] genericArguments)
        {
            Debug.Assert(entry.Lifetime.HasValue);

            var specialied = (ServiceEntry) entry.GetType().CreateInstance
            (
                new []
                {
                    typeof(Type),
                    typeof(Lifetime?),
                    typeof(Type)
                }, 
                entry.Interface.MakeGenericType(genericArguments), 
                entry.Lifetime.Value, 
                entry.Implementation.MakeGenericType(genericArguments)
            );

            //
            // Ha a generikus bejegyzesunk ITypeResolver-t hasznal akkor itt van az elso alkalom hogy
            // validaljunk.
            //

            if (entry.IsLazy) specialied.CheckValid(); 

            return specialied;
        }
    }
}
