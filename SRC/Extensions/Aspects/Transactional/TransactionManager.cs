/********************************************************************************
* TransactionManager.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Data;
using System.Reflection;

namespace Solti.Utils.DI.Extensions.Aspects
{
    using Proxy;

    /// <summary>
    /// Defines a generic transaction manager proxy.
    /// </summary>
    /// <remarks>You should never instantiate this class directly.</remarks>
    public class TransactionManager<TInterface> : InterfaceInterceptor<TInterface> where TInterface : class
    {
        private readonly Lazy<IDbConnection> FConnection;

        /// <summary>
        /// The <see cref="IDbConnection"/> to be transacted.
        /// </summary>
        public IDbConnection Connection => FConnection.Value;

        /// <summary>
        /// Creates a new <see cref="TransactionManager{TInterface}"/> instance.
        /// </summary>
        public TransactionManager(TInterface target, Lazy<IDbConnection> dbConn) : base(target ?? throw new ArgumentNullException(nameof(target))) =>
            FConnection = dbConn ?? throw new ArgumentNullException(nameof(dbConn));

        /// <summary>
        /// See the <see cref="InterfaceInterceptor{TInterface}.Invoke(MethodInfo, object[], MemberInfo)"/> method.
        /// </summary>
        public override object Invoke(MethodInfo method, object[] args, MemberInfo extra)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            TransactionalAttribute ta = method.GetCustomAttribute<TransactionalAttribute>();
            if (ta == null)
                return base.Invoke(method, args, extra);

            IDbTransaction transaction = Connection.BeginTransaction(ta.IsolationLevel);
            try
            {
                object result = base.Invoke(method, args, extra);
                transaction.Commit();
                return result;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
