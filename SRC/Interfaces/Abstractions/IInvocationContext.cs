/********************************************************************************
* IInvocationContext.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Reflection;

namespace Solti.Utils.DI.Interfaces
{
    /// <summary>
    /// Contains the related context of a perticular method invocation
    /// </summary>
    public interface IInvocationContext
    {
        /// <summary>
        /// The arguments passed by the caller.
        /// </summary>
        public object?[] Args { get; }

        /// <summary>
        /// The concrete method behind the <see cref="InterfaceMember"/>.
        /// </summary>
        public MethodInfo InterfaceMethod { get; }

        /// <summary>
        /// The member (property, event or method) that is being invoked.
        /// </summary> 
        public MemberInfo InterfaceMember { get; }

        /// <summary>
        /// The concrete method behind the <see cref="TargetMember"/>.
        /// </summary>  
        public MethodInfo TargeteMethod { get; }

        /// <summary>
        /// The member (property, event or method) that is being targeted.
        /// </summary>    
        public MemberInfo TargetMember { get; }
    }
}