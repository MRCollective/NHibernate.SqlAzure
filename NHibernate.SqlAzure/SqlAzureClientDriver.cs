using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling;
using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.SqlAzure;
using Microsoft.Practices.TransientFaultHandling;
using NHibernate.Driver;

namespace NHibernate.SqlAzure
{
    public class SqlAzureClientDriver : Sql2008ClientDriver//, IEmbeddedBatcherFactoryProvider
    {
        public override IDbConnection CreateConnection()
        {
            var retryStrategies = new List<RetryStrategy>();
            const string incremental = "Incremental Retry Strategy";
            const string interval = "Fixed Interval Retry Strategy";
            const string backoff = "Backoff Retry Strategy";
            retryStrategies.Add(new Incremental(incremental, 10, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));
            retryStrategies.Add(new FixedInterval(interval, 10, TimeSpan.FromSeconds(1)));
            retryStrategies.Add(new ExponentialBackoff(backoff, 10, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(10), false));

            var retryManager = new RetryManagerImpl(retryStrategies, interval, backoff, incremental, interval, interval, interval);

            return new ReliableSqlConnection(null, retryManager.GetDefaultSqlConnectionRetryPolicy(), retryManager.GetDefaultSqlCommandRetryPolicy());
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