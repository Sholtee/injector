/********************************************************************************
* TransientServiceEntry.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal sealed partial class TransientServiceEntry
    {
        private static readonly MethodInfo
            FCreateInstance = MethodInfoExtractor.Extract<IInstanceFactory>(fact => fact.CreateInstance(null!));

        public override Expression CreateResolverExpression(ParameterExpression injector!!, ref int slot)
        {
            ParameterExpression instanceFactory = Expression.Parameter(typeof(IInstanceFactory), nameof(instanceFactory));

            return Expression.Block
            (
                typeof(object),
                new ParameterExpression[] { instanceFactory },
                Expression.Assign
                (
                    instanceFactory,
                    Expression.Convert(injector, typeof(IInstanceFactory))
                ),
                Expression.Call(instanceFactory, FCreateInstance, Expression.Constant(this))
            );                  
        }
    }
}