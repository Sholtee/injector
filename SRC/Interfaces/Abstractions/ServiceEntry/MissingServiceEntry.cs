/********************************************************************************
* MissingServiceEntry.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics;
using System.Linq;

namespace Solti.Utils.DI.Interfaces
{
    using Properties;

    /// <summary>
    /// Describes a missing service.
    /// </summary>
    /// <remarks>This entry cannot be instantiated.</remarks>
    public sealed class MissingServiceEntry : AbstractServiceEntry
    {
        /// <summary>
        /// Creates a new <see cref="MissingServiceEntry"/> instance.
        /// </summary>
        public MissingServiceEntry(Type @interface, string? name) : base(@interface, name) { }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public override AbstractServiceEntry CopyTo(IServiceRegistry owner) => throw new NotImplementedException();

        /// <summary>
        /// Throws a <see cref="ServiceNotFoundException"/>.
        /// </summary>
        public override object CreateInstance(IInjector scope)
        {
            if (scope is null)
                throw new ArgumentNullException(nameof(scope));

            //                                          !!FIGYELEM!!
            //
            // A szerviz utvonal elerese teljesitmeny optimalizacio miatt csak az elso sikeres peldanyositasig lehetseges
            // -> Eles szervizek NE fuggjenek az IServicePath interface-tol.
            //

            IServicePath path = scope.Get<IServicePath>();
            Debug.Assert(path.Last == this, "Wrong path");

            ServiceNotFoundException ex = new(string.Format(Resources.Culture, Resources.SERVICE_NOT_FOUND, ToString(shortForm: true)));

            ex.Data["requested"] = this;
            ex.Data["requestor"] = path.Length > 1
                ? path.Reverse().Skip(1).First() // TODO: LINQ nelkul?
                : null;
#if DEBUG
            ex.Data[nameof(scope)] = scope;
#endif
            throw ex;
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public override object GetSingleInstance() => throw new NotImplementedException();
    }
}