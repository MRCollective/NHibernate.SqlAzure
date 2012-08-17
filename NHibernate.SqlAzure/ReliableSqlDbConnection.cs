using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.SqlAzure;

namespace NHibernate.SqlAzure
{
    /// <summary>
    /// Wrap ReliableSqlConnection in a class that extends <see cref="DbConnection"/>
    /// so internal type casts within NHibernate don't fail.
    /// </summary>
    public class ReliableSqlDbConnection : DbConnection
    {
        public ReliableSqlConnection ReliableConnection { get; set; }

        public ReliableSqlDbConnection(ReliableSqlConnection connection)
        {
            ReliableConnection = connection;
        }

        public static explicit operator SqlConnection(ReliableSqlDbConnection connection)
        {
            return connection.ReliableConnection.Current;
        }

        public new void Dispose()
        {
            ReliableConnection.Dispose();
            base.Dispose();
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            return (DbTransaction) ReliableConnection.BeginTransaction(isolationLevel);
        }

        public override void Close()
        {
            ReliableConnection.Close();
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
    }
}