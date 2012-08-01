using System.Data;
using System.Data.SqlClient;
using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.SqlAzure;
using NHibernate.AdoNet;
using NHibernate.Driver;

namespace NHibernate.SqlAzure
{
    public class SqlAzureDriver : Sql2008ClientDriver//, IEmbeddedBatcherFactoryProvider
    {
        public override IDbConnection CreateConnection()
        {
            return new ReliableSqlConnection(null);
        }

        /*public override IDbCommand CreateCommand()
        {
            return new SqlAzureCommand();
        }*/

        /*public System.Type BatcherFactoryClass
        {
            get { return typeof(SqlAzureBatchingFactory); }
        }*/
    }


    /*public class SqlAzureCommand : IDbCommand
    {
        private readonly System.Data.SqlClient.SqlCommand _command;

        public SqlAzureCommand()
        {
            _command = new System.Data.SqlClient.SqlCommand();
        }

        public void Dispose()
        {
            _command.Dispose();
        }

        public void Prepare()
        {
            _command.Prepare();
        }

        public void Cancel()
        {
            _command.Cancel();
        }

        public IDbDataParameter CreateParameter()
        {
            return _command.CreateParameter();
        }

        public int ExecuteNonQuery()
        {
            return ReliableConnection.ExecuteCommand(_command);
        }

        public IDataReader ExecuteReader()
        {
            return ReliableConnection.ExecuteCommand<IDataReader>(_command);
        }

        public IDataReader ExecuteReader(CommandBehavior behavior)
        {
            return ReliableConnection.ExecuteCommand<IDataReader>(_command, behavior);
        }

        public object ExecuteScalar()
        {
            return ReliableConnection.ExecuteCommand<int>(_command);
        }

        public IDbConnection Connection
        {
            get { return _command.Connection; }
            set
            {
                ReliableConnection = (ReliableSqlConnection)value;
                _command.Connection = ReliableConnection.Current;
            }
        }

        public ReliableSqlConnection ReliableConnection { get; set; }

        public IDbTransaction Transaction
        {
            get { return _command.Transaction; }
            set { _command.Transaction = (SqlTransaction)value; }
        }

        public string CommandText
        {
            get { return _command.CommandText; }
            set { _command.CommandText = value; }
        }

        public int CommandTimeout
        {
            get { return _command.CommandTimeout; }
            set { _command.CommandTimeout = value; }
        }

        public CommandType CommandType
        {
            get { return _command.CommandType; }
            set { _command.CommandType = value; }
        }

        public IDataParameterCollection Parameters
        {
            get { return _command.Parameters; }
        }

        public UpdateRowSource UpdatedRowSource
        {
            get { return _command.UpdatedRowSource; }
            set { _command.UpdatedRowSource = value; }
        }
    }*/
}