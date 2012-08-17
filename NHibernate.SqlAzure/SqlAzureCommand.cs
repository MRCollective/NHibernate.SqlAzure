using System.Data;
using System.Data.SqlClient;
using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.SqlAzure;

namespace NHibernate.SqlAzure
{
    public class SqlAzureCommand : IDbCommand
    {
        public System.Data.SqlClient.SqlCommand Current { get; private set; }

        public SqlAzureCommand()
        {
            Current = new System.Data.SqlClient.SqlCommand();
        }

        public static explicit operator System.Data.SqlClient.SqlCommand(SqlAzureCommand command)
        {
            return command.Current;
        }

        public void Dispose()
        {
            Current.Dispose();
        }

        public void Prepare()
        {
            Current.Prepare();
        }

        public void Cancel()
        {
            Current.Cancel();
        }

        public IDbDataParameter CreateParameter()
        {
            return Current.CreateParameter();
        }

        public int ExecuteNonQuery()
        {
            return ReliableConnection.ExecuteCommand(Current);
        }

        public IDataReader ExecuteReader()
        {
            return ReliableConnection.ExecuteCommand<IDataReader>(Current);
        }

        public IDataReader ExecuteReader(CommandBehavior behavior)
        {
            return ReliableConnection.ExecuteCommand<IDataReader>(Current, behavior);
        }

        public object ExecuteScalar()
        {
            return ReliableConnection.ExecuteCommand<int>(Current);
        }

        public IDbConnection Connection
        {
            get { return Current.Connection; }
            set
            {
                ReliableConnection = ((ReliableSqlDbConnection)value).ReliableConnection;
                Current.Connection = ReliableConnection.Current;
            }
        }

        public ReliableSqlConnection ReliableConnection { get; set; }

        public IDbTransaction Transaction
        {
            get { return Current.Transaction; }
            set { Current.Transaction = (SqlTransaction)value; }
        }

        public string CommandText
        {
            get { return Current.CommandText; }
            set { Current.CommandText = value; }
        }

        public int CommandTimeout
        {
            get { return Current.CommandTimeout; }
            set { Current.CommandTimeout = value; }
        }

        public CommandType CommandType
        {
            get { return Current.CommandType; }
            set { Current.CommandType = value; }
        }

        public IDataParameterCollection Parameters
        {
            get { return Current.Parameters; }
        }

        public UpdateRowSource UpdatedRowSource
        {
            get { return Current.UpdatedRowSource; }
            set { Current.UpdatedRowSource = value; }
        }
    }
}