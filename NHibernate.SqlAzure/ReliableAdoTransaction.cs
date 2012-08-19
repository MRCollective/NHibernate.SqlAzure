using System.Data;
using NHibernate.Engine;
using NHibernate.Transaction;

namespace NHibernate.SqlAzure
{
    /// <summary>
    /// Provides a transaction implementation that includes transient fault-handling retry logic.
    /// </summary>
    public class ReliableAdoTransaction : AdoTransaction, ITransaction
    {
        private readonly ReliableSqlDbConnection _connection;

        /// <summary>
        /// Constructs a <see cref="ReliableAdoTransaction"/>.
        /// </summary>
        /// <param name="session">NHibernate session to use.</param>
        public ReliableAdoTransaction(ISessionImplementor session) : base(session)
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

        /// <summary>
        /// Executes the given action with the command retry policy on the given <see cref="ReliableSqlDbConnection"/>.
        /// </summary>
        /// <param name="connection">The reliable connection</param>
        /// <param name="action">The action to execute</param>
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