using NHibernate.Engine;
using NHibernate.Engine.Transaction;
using NHibernate.Transaction;

namespace NHibernate.SqlAzure
{
    public class RetryingAdoNetTransactionFactory : AdoNetTransactionFactory, ITransactionFactory
    {
        public new ITransaction CreateTransaction(ISessionImplementor session)
        {
            return new RetryingAdoTransaction(session);
        }

        public new void ExecuteWorkInIsolation(ISessionImplementor session, IIsolatedWork work, bool transacted)
        {
            var connection = (ReliableSqlDbConnection)session.Connection;

            RetryingAdoTransaction.ExecuteWithRetry(connection,
                () => base.ExecuteWorkInIsolation(session, work, transacted)
            );
        }
    }
}
