/********************************************************************************
* SingletonServiceEntryBase.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Linq.Expressions;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;

    internal abstract partial class SingletonServiceEntryBase
    {
        protected static readonly MethodInfo
            FGetOrCreateInstance = MethodInfoExtractor.Extract<IInstanceFactory>(fact => fact.GetOrCreateInstance(null!, 0));

        protected static readonly PropertyInfo
            FSuper = PropertyInfoExtractor.Extract<IInstanceFactory, IInstanceFactory?>(fact => fact.Super);

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
                Expression.Call
                (
                    Expression.Coalesce
                    (
                        Expression.Property(instanceFactory, FSuper),
                        instanceFactory
                    ),
                    FGetOrCreateInstance,
                    Expression.Constant(this),
                    Expression.Constant(slot++)
                )
            );
        }
    }
}