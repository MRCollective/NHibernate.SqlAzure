using System.Data;
using NHibernate.Engine;
using NHibernate.Transaction;

namespace NHibernate.SqlAzure
{
    public class RetryingAdoTransaction : AdoTransaction, ITransaction
    {
        private ReliableSqlDbConnection _connection;

        public RetryingAdoTransaction(ISessionImplementor session) : base(session)
        {
            _connection = (ReliableSqlDbConnection) session.Connection;
        }

        public new void Begin()
        {
            Begin(IsolationLevel.Unspecified);
        }

        public new void Begin(IsolationLevel isolationLevel)
        {
            _connection.ReliableConnection.CommandRetryPolicy.ExecuteAction(
                () => base.Begin(isolationLevel)
            );
        }
    }
}