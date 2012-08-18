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
            ExecuteWithRetry(_connection, () => base.Begin(isolationLevel));
        }

        public static void ExecuteWithRetry(ReliableSqlDbConnection connection, System.Action action)
        {
            connection.ReliableConnection.CommandRetryPolicy.ExecuteAction(() =>
                {
                    if (connection.State != ConnectionState.Open)
                        connection.Open();

                    action();
                }
            );
        }
    }
}