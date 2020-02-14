/********************************************************************************
* ServiceDefinitionComparer.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
namespace Solti.Utils.DI.Internals
{
    internal sealed class ServiceDefinitionComparer : ComparerBase<ServiceDefinitionComparer, IServiceDefinition>
    {
        public override int GetHashCode(IServiceDefinition obj) =>
#if NETSTANDARD1_6 || NETSTANDARD2_0
            new { obj.Interface, obj.Name, obj.Owner, obj.Lifetime, obj.Implementation, obj.Factory }.GetHashCode()
#else
            System.HashCode.Combine(obj.Interface, obj.Name, obj.Owner, obj.Lifetime, obj.Implementation, obj.Factory)
#endif
            ;
    }
}
