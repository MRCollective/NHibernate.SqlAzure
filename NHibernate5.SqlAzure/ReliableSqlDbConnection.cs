using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace NHibernate.SqlAzure
{
    /// <summary>
    /// Wrap <see cref="ReliableSqlConnection"/> in a class that extends <see cref="DbConnection"/>
    /// so internal type casts within NHibernate don't fail.
    /// </summary>
    public class ReliableSqlDbConnection : DbConnection
    {
        bool disposed = false;
        /// <summary>
        /// The underlying <see cref="ReliableSqlConnection"/>.
        /// </summary>
        public ReliableSqlConnection ReliableConnection { get; set; }

        /// <summary>
        /// Constructs a <see cref="ReliableSqlDbConnection"/> to wrap around the given <see cref="ReliableSqlConnection"/>.
        /// </summary>
        /// <param name="connection">The <see cref="ReliableSqlConnection"/> to wrap</param>
        public ReliableSqlDbConnection(ReliableSqlConnection connection)
        {
            ReliableConnection = connection;
        }

        /// <summary>
        /// Explicit type-casting between <see cref="ReliableSqlDbConnection"/> and <see cref="ReliableSqlConnection"/>.
        /// </summary>
        /// <param name="connection">The <see cref="ReliableSqlDbConnection"/> being casted</param>
        /// <returns>The underlying <see cref="ReliableSqlConnection"/></returns>
        public static explicit operator SqlConnection(ReliableSqlDbConnection connection)
        {
            return connection.ReliableConnection.Current;
        }

        protected override void Dispose(bool disposing) {
            if (disposed)
                return;
            if (disposing) {
                ReliableConnection.Dispose();
            }
            disposed = true;
            base.Dispose(disposing);
        }

        #region Wrapping code
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return (DbTransaction) ReliableConnection.BeginTransaction(isolationLevel);
        }

        public override void Close()
        {
            ReliableConnection.Close();
        }

        public override DataTable GetSchema()
        {
            return ReliableConnection.ConnectionRetryPolicy.ExecuteAction (
                () => ReliableConnection.Current.GetSchema()
            );
        }

        public override DataTable GetSchema(string collectionName)
        {
            return ReliableConnection.ConnectionRetryPolicy.ExecuteAction (
                () => ReliableConnection.Current.GetSchema(collectionName)
            );
        }

        public override DataTable GetSchema(string collectionName, string[] restrictionValues)
        {
            return ReliableConnection.ConnectionRetryPolicy.ExecuteAction (
                () => ReliableConnection.Current.GetSchema(collectionName, restrictionValues)
            );
        }

        public override void ChangeDatabase(string databaseName)
        {
            ReliableConnection.ChangeDatabase(databaseName);
        }

        protected override DbCommand CreateDbCommand()
        {
            return ReliableConnection.CreateCommand();
        }

        public override void Open()
        {
            ReliableConnection.Open();
        }

        public override string ConnectionString { get { return ReliableConnection.ConnectionString; } set { ReliableConnection.ConnectionString = value; } }
        public override int ConnectionTimeout { get { return ReliableConnection.ConnectionTimeout; } }
        public override string Database { get { return ReliableConnection.Database; } }
        public override string DataSource { get { return ""; } } 
        public override string ServerVersion { get { return ""; } }
        public override ConnectionState State { get { return ReliableConnection.State; } }
        #endregion
    }
}