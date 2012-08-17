// This file was copied from NHibernate.AdoNet.SqlClientBatchingBatcherFactory, but modified to use SqlAzureClientSqlCommandSet
using System;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;
using NHibernate.AdoNet;
using NHibernate.AdoNet.Util;
using NHibernate.Exceptions;
using NHibernate.Util;

namespace NHibernate.SqlAzure
{
    public class SqlAzureClientBatchingBatcher : AbstractBatcher
    {
        private readonly ConnectionManager _connectionManager;
        private int _batchSize;
        private int _totalExpectedRowsAffected;
        private SqlAzureClientSqlCommandSet _currentBatch;
        private StringBuilder _currentBatchCommandsLog;
        private readonly int _defaultTimeout;

        public SqlAzureClientBatchingBatcher(ConnectionManager connectionManager, IInterceptor interceptor)
            : base(connectionManager, interceptor)
        {
            _connectionManager = connectionManager;
            _batchSize = Factory.Settings.AdoBatchSize;
            _defaultTimeout = PropertiesHelper.GetInt32(Cfg.Environment.CommandTimeout, Cfg.Environment.Properties, -1);

            _currentBatch = CreateConfiguredBatch();
            //we always create this, because we need to deal with a scenario in which
            //the user change the logging configuration at runtime. Trying to put this
            //behind an if(log.IsDebugEnabled) will cause a null reference exception 
            //at that point.
            _currentBatchCommandsLog = new StringBuilder().AppendLine("Batch commands:");
        }

        public override int BatchSize
        {
            get { return _batchSize; }
            set { _batchSize = value; }
        }

        protected override int CountOfStatementsInCurrentBatch
        {
            get { return _currentBatch.CountOfCommands; }
        }

        public override void AddToBatch(IExpectation expectation)
        {
            _totalExpectedRowsAffected += expectation.ExpectedRowCount;
            IDbCommand batchUpdate = CurrentCommand;
            Driver.AdjustCommand(batchUpdate);
            string lineWithParameters = null;
            var sqlStatementLogger = Factory.Settings.SqlStatementLogger;
            if (sqlStatementLogger.IsDebugEnabled || Log.IsDebugEnabled)
            {
                lineWithParameters = sqlStatementLogger.GetCommandLineWithParameters(batchUpdate);
                var formatStyle = sqlStatementLogger.DetermineActualStyle(FormatStyle.Basic);
                lineWithParameters = formatStyle.Formatter.Format(lineWithParameters);
                _currentBatchCommandsLog.Append("command ")
                    .Append(_currentBatch.CountOfCommands)
                    .Append(":")
                    .AppendLine(lineWithParameters);
            }
            if (Log.IsDebugEnabled)
            {
                Log.Debug("Adding to batch:" + lineWithParameters);
            }
            _currentBatch.Append((System.Data.SqlClient.SqlCommand)(SqlAzureCommand)batchUpdate);

            if (_currentBatch.CountOfCommands >= _batchSize)
            {
                ExecuteBatchWithTiming(batchUpdate);
            }
        }

        /// <summary>
        /// Prepares the <see cref="IDbCommand"/> for execution in the database.
        /// </summary>
        /// <remarks>
        /// This takes care of hooking the <see cref="IDbCommand"/> up to an <see cref="IDbConnection"/>
        /// and <see cref="IDbTransaction"/> if one exists.  It will call <c>Prepare</c> if the Driver
        /// supports preparing commands.
        /// </remarks>
        protected new void Prepare(IDbCommand cmd)
        {
            try
            {
                var sessionConnection = (ReliableSqlDbConnection)_connectionManager.GetConnection();

                if (cmd.Connection != null)
                {
                    // make sure the commands connection is the same as the Sessions connection
                    // these can be different when the session is disconnected and then reconnected
                    if (cmd.Connection != sessionConnection)
                    {
                        cmd.Connection = (System.Data.SqlClient.SqlConnection) sessionConnection;
                    }
                }
                else
                {
                    cmd.Connection = (System.Data.SqlClient.SqlConnection) sessionConnection;
                }

                _connectionManager.Transaction.Enlist(cmd);
                Driver.PrepareCommand(cmd);
            }
            catch (InvalidOperationException ioe)
            {
                throw new ADOException("While preparing " + cmd.CommandText + " an error occurred", ioe);
            }
        }

        protected override void DoExecuteBatch(IDbCommand ps)
        {
            Log.DebugFormat("Executing batch");
            CheckReaders();
            Prepare(_currentBatch.BatchCommand);
            if (Factory.Settings.SqlStatementLogger.IsDebugEnabled)
            {
                Factory.Settings.SqlStatementLogger.LogBatchCommand(_currentBatchCommandsLog.ToString());
                _currentBatchCommandsLog = new StringBuilder().AppendLine("Batch commands:");
            }

            int rowsAffected;
            try
            {
                rowsAffected = _currentBatch.ExecuteNonQuery();
            }
            catch (DbException e)
            {
                throw ADOExceptionHelper.Convert(Factory.SQLExceptionConverter, e, "could not execute batch command.");
            }

            Expectations.VerifyOutcomeBatched(_totalExpectedRowsAffected, rowsAffected);

            _currentBatch.Dispose();
            _totalExpectedRowsAffected = 0;
            _currentBatch = CreateConfiguredBatch();
        }

        private SqlAzureClientSqlCommandSet CreateConfiguredBatch()
        {
            var result = new SqlAzureClientSqlCommandSet();
            if (_defaultTimeout > 0)
            {
                try
                {
                    result.CommandTimeout = _defaultTimeout;
                }
                catch (Exception e)
                {
                    if (Log.IsWarnEnabled)
                    {
                        Log.Warn(e.ToString());
                    }
                }
            }

            return result;
        }
    }
}