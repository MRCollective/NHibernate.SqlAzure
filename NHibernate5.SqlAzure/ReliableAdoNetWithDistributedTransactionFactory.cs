using NHibernate.Engine;
using NHibernate.Engine.Transaction;
using NHibernate.Transaction;

namespace NHibernate.SqlAzure
{
    /// <summary>
    /// An NHibernate transaction factory that provides retry logic for transient errors when executing transactions.
    /// It is aware of Distributed Transactions.
    /// </summary>
    /// <remarks>
    /// Requires the connection to be a <see cref="ReliableSqlDbConnection"/>
    /// </remarks>
    public class ReliableAdoNetWithDistributedTransactionFactory : AdoNetWithSystemTransactionFactory, ITransactionFactory
    {
        public new ITransaction CreateTransaction(ISessionImplementor session)
        {
            return new ReliableAdoTransaction(session);
        }

        /// <summary>
        /// Executes some work in isolation.
        /// </summary>
        /// <param name="session">The NHibernate session</param>
        /// <param name="work">The work to execute</param>
        /// <param name="transacted">Whether or not to wrap the work in a transaction</param>
        public new void ExecuteWorkInIsolation(ISessionImplementor session, IIsolatedWork work, bool transacted)
        {
            var connection = (ReliableSqlDbConnection) session.Connection;

            ReliableAdoTransaction.ExecuteWithRetry(connection,
                () => base.ExecuteWorkInIsolation(session, work, transacted)
            );
        }
    }
}
