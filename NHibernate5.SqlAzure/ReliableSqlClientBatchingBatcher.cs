// Parts of this file were copied from NHibernate.AdoNet.SqlClientBatchingBatcherFactory, but modified to use ReliableSqlDbConnection
// The #regions indicate the copied code
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.AdoNet;
using NHibernate.AdoNet.Util;
using NHibernate.Exceptions;
using NHibernate.SqlCommand;
using NHibernate.Util;

namespace NHibernate.SqlAzure
{
    /// <summary>
    /// Exposes <see cref="SqlClientBatchingBatcher"/> functionality when a <see cref="ReliableSqlDbConnection"/>
    /// connection is being used.
    /// </summary>
    public class ReliableSqlClientBatchingBatcher : AbstractBatcher
    {
        private readonly int? _maxNumberOfParameters;
        private BatchingCommandSet _currentBatch;
        private int _totalExpectedRowsAffected;
        private StringBuilder _currentBatchCommandsLog;
        private readonly int _defaultTimeout;
        private readonly ConnectionManager _connectionManager;

        public ReliableSqlClientBatchingBatcher(ConnectionManager connectionManager, IInterceptor interceptor)
            : base(connectionManager, interceptor)
        {
            BatchSize = Factory.Settings.AdoBatchSize;
            _currentBatch = new BatchingCommandSet(this, Factory.Dialect.StatementTerminator);
            _maxNumberOfParameters = Factory.Dialect.MaxNumberOfParameters;
            _defaultTimeout = PropertiesHelper.GetInt32(Cfg.Environment.CommandTimeout, Cfg.Environment.Properties, -1);
            _connectionManager = connectionManager;

            // We always create this, because we need to deal with a scenario in which
            // the user change the logging configuration at runtime. Trying to put this
            // behind an if(log.IsDebugEnabled) will cause a null reference exception 
            // at that point.
            _currentBatchCommandsLog = new StringBuilder().AppendLine("Batch commands:");
        }

        public sealed override int BatchSize { get; set; }

        protected override int CountOfStatementsInCurrentBatch => _currentBatch.CountOfCommands;

        public override void AddToBatch(IExpectation expectation)
        {
            #region NHibernate code
            _totalExpectedRowsAffected += expectation.ExpectedRowCount;
            DbCommand batchUpdate = CurrentCommand;
            Driver.AdjustCommand(batchUpdate);
            string lineWithParameters = null;
            var sqlStatementLogger = Factory.Settings.SqlStatementLogger;
            if (sqlStatementLogger.IsDebugEnabled || Log.IsDebugEnabled())
            {
                lineWithParameters = sqlStatementLogger.GetCommandLineWithParameters(batchUpdate);
                var formatStyle = sqlStatementLogger.DetermineActualStyle(FormatStyle.Basic);
                lineWithParameters = formatStyle.Formatter.Format(lineWithParameters);
                _currentBatchCommandsLog.Append("command ")
                    .Append(_currentBatch.CountOfCommands)
                    .Append(":")
                    .AppendLine(lineWithParameters);
            }
            if (Log.IsDebugEnabled())
            {
                Log.Debug("Adding to batch:" + lineWithParameters);
            }
            #endregion

            _currentBatch.Append();

            #region NHibernate code
            if (_currentBatch.CountOfCommands >= BatchSize)
            {
                ExecuteBatchWithTiming(batchUpdate);
            }
            #endregion
        }

        // Need this method call in this class rather than the base class to ensure Prepare is called... if only it was virtual :(
        protected void ExecuteBatch(IDbCommand ps)
        {
            #region NHibernate code
            Log.Debug("Executing batch");
            CheckReaders();
            Prepare(CurrentCommand);
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

            _totalExpectedRowsAffected = 0;
            _currentBatch = CreateConfiguredBatch();
            #endregion
        }

        /// <summary>
        /// Prepares the <see cref="DbCommand"/> for execution in the database.
        /// </summary>
        /// <remarks>
        /// This takes care of hooking the <see cref="DbCommand"/> up to an <see cref="DbConnection"/>
        /// and <see cref="DbTransaction"/> if one exists.  It will call <c>Prepare</c> if the Driver
        /// supports preparing commands.
        /// </remarks>
        protected new void Prepare(DbCommand cmd)
        {
            try
            {
                var sessionConnection = (ReliableSqlDbConnection)_connectionManager.GetConnection();

                #region NHibernate code
                if (cmd.Connection != null)
                {
                    // make sure the commands connection is the same as the Sessions connection
                    // these can be different when the session is disconnected and then reconnected
                    if (cmd.Connection != sessionConnection)
                    {
                        cmd.Connection = (System.Data.SqlClient.SqlConnection)sessionConnection;
                    }
                }
                else
                {
                    cmd.Connection = (System.Data.SqlClient.SqlConnection)sessionConnection;
                }

                _connectionManager.Transaction.Enlist(cmd);
                Driver.PrepareCommand(cmd);
                #endregion
            }
            catch (InvalidOperationException ioe)
            {
                #region NHibernate code
                throw new ADOException("While preparing " + cmd.CommandText + " an error occurred", ioe);
                #endregion
            }
        }

        protected override void DoExecuteBatch(DbCommand ps)
        {
            if (_currentBatch.CountOfCommands == 0)
            {
                Expectations.VerifyOutcomeBatched(_totalExpectedRowsAffected, 0, ps);
                return;
            }
            try
            {
                Log.Debug("Executing batch");
                CheckReaders();
                if (Factory.Settings.SqlStatementLogger.IsDebugEnabled)
                {
                    Factory.Settings.SqlStatementLogger.LogBatchCommand(_currentBatchCommandsLog.ToString());
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

                Expectations.VerifyOutcomeBatched(_totalExpectedRowsAffected, rowsAffected, ps);
            }
            finally
            {
                ClearCurrentBatch();
            }
        }

        protected override Task DoExecuteBatchAsync(DbCommand ps, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task AddToBatchAsync(IExpectation expectation, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private BatchingCommandSet CreateConfiguredBatch()
        {
            var result = new BatchingCommandSet(this, Factory.Dialect.StatementTerminator);
            if (_defaultTimeout > 0)
            {
                try
                {
                    result.CommandTimeout = _defaultTimeout;
                }
                catch (Exception e)
                {
                    if (Log.IsWarnEnabled())
                    {
                        Log.Warn(e, e.ToString());
                    }
                }
            }

            return result;
        }

        private void ClearCurrentBatch()
        {
            _currentBatch = null;
            _totalExpectedRowsAffected = 0;
            _currentBatch = CreateConfiguredBatch();

            if (Factory.Settings.SqlStatementLogger.IsDebugEnabled)
            {
                _currentBatchCommandsLog = new StringBuilder().AppendLine("Batch commands:");
            }
        }

        private partial class BatchingCommandSet
        {
            private readonly string _statementTerminator;
            private readonly ReliableSqlClientBatchingBatcher _batcher;
            private readonly SqlStringBuilder _sql = new SqlStringBuilder();
            private readonly List<SqlTypes.SqlType> _sqlTypes = new List<SqlTypes.SqlType>();
            private readonly List<BatchParameter> _parameters = new List<BatchParameter>();
            private CommandType _commandType;

            private class BatchParameter
            {
                public ParameterDirection Direction { get; set; }

                public byte Precision { get; set; }

                public byte Scale { get; set; }

                public int Size { get; set; }

                public object Value { get; set; }
            }

            public BatchingCommandSet(ReliableSqlClientBatchingBatcher batcher, char statementTerminator)
            {
                _batcher = batcher;
                _statementTerminator = statementTerminator.ToString();
            }

            public int CountOfCommands { get; private set; }

            public int CountOfParameters { get; private set; }

            public int CommandTimeout { get; set; }

            public void Append()
            {
                if (CountOfCommands > 0)
                {
                    _sql.Add(_statementTerminator);
                }
                else
                {
                    _commandType = _batcher.CurrentCommand.CommandType;
                }

                _sql.Add(_batcher.CurrentCommandSql.Copy());
                _sqlTypes.AddRange(_batcher.CurrentCommandParameterTypes);

                foreach (DbParameter parameter in _batcher.CurrentCommand.Parameters)
                {
                    _parameters.Add(new BatchParameter
                    {
                        Direction = parameter.Direction,
                        Precision = parameter.Precision,
                        Scale = parameter.Scale,
                        Size = parameter.Size,
                        Value = parameter.Value
                    });
                }
                CountOfCommands++;
                CountOfParameters += _batcher.CurrentCommand.Parameters.Count;
            }

            public int ExecuteNonQuery()
            {
                if (CountOfCommands == 0)
                {
                    return 0;
                }
                var batcherCommand = _batcher.Driver.GenerateCommand(
                    _commandType,
                    _sql.ToSqlString(),
                    _sqlTypes.ToArray()
                );
                for (var i = 0; i < _parameters.Count; i++)
                {
                    var parameter = _parameters[i];
                    var cmdParam = batcherCommand.Parameters[i];
                    cmdParam.Value = parameter.Value;
                    cmdParam.Direction = parameter.Direction;
                    cmdParam.Precision = parameter.Precision;
                    cmdParam.Scale = parameter.Scale;
                    cmdParam.Size = parameter.Size;
                }
                _batcher.Prepare(batcherCommand);
                return batcherCommand.ExecuteNonQuery();
            }

            public void Clear()
            {
                CountOfParameters = 0;
                CountOfCommands = 0;
                _sql.Clear();
                _sqlTypes.Clear();
                _parameters.Clear();
            }
        }

    }
}