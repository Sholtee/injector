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
        public static Func<IInjector, Type, object> CreateFactory(this ServiceEntry entry) => Resolver.Get(entry.Implementation).ConvertToFactory();

        public static void SetFactory(this ServiceEntry entry) => entry.Factory = entry.CreateFactory();

        public static void CheckValid(this ServiceEntry entry)
        {
            Type
                @interface     = entry.Interface,
                implementation = entry.Implementation;

            if (!@interface.IsInterfaceOf(implementation))
                throw new InvalidOperationException(string.Format(Resources.NOT_ASSIGNABLE, @interface, implementation));

            implementation.GetApplicableConstructor(); // validal is (mukodik generikus bejegyzesre is)
        }

        public static ServiceEntry Specialize(this ServiceEntry entry, params Type[] genericArguments)
        {
            Debug.Assert(entry.Lifetime.HasValue, "Entries containing Instance definition can not be specialized.");

            var specialied = new ServiceEntry(entry.Interface.MakeGenericType(genericArguments), entry.Lifetime.Value, entry.Implementation.MakeGenericType(genericArguments));

            //
            // Ha a generikus bejegyzesunk ITypeResolver-t hasznal akkor itt van az elso alkalom hogy
            // validaljunk.
            //

            specialied.CheckValid(); 
            specialied.SetFactory();

            return specialied;
        }
    }
}
