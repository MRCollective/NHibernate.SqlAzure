// Parts of this file were copied from NHibernate.AdoNet.SqlClientBatchingBatcherFactory, but modified to use ReliableSqlDbConnection
// The #regions indicate the copied code
using System;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.AdoNet;
using NHibernate.AdoNet.Util;
using NHibernate.Exceptions;

namespace NHibernate.SqlAzure
{
    /// <summary>
    /// Exposes <see cref="SqlClientBatchingBatcher"/> functionality when a <see cref="ReliableSqlDbConnection"/>
    /// connection is being used.
    /// </summary>
    public class ReliableSqlClientBatchingBatcher : SqlClientBatchingBatcher
    {
        #region Impersonate private fields in base class
        private readonly ConnectionManager _connectionManager;
        private readonly FieldInfo _totalExpectedRowsAffectedField = typeof(SqlClientBatchingBatcher)
            .GetField("_totalExpectedRowsAffected", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly FieldInfo _currentBatchField = typeof (SqlClientBatchingBatcher)
            .GetField("_currentBatch", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly FieldInfo _currentBatchCommandsLogField = typeof(SqlClientBatchingBatcher)
            .GetField("_currentBatchCommandsLog", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly MethodInfo _createConfiguredBatchMethod = typeof (SqlClientBatchingBatcher)
            .GetMethod("CreateConfiguredBatch", BindingFlags.Instance | BindingFlags.NonPublic);

        // ReSharper disable InconsistentNaming
        private int _totalExpectedRowsAffected
        {
            get { return (int)_totalExpectedRowsAffectedField.GetValue(this); }
            set { _totalExpectedRowsAffectedField.SetValue(this, value); }
        }
        private SqlClientSqlCommandSet _currentBatch
        {
            get { return (SqlClientSqlCommandSet)_currentBatchField.GetValue(this); }
            set { _currentBatchField.SetValue(this, value); }
        }
        private StringBuilder _currentBatchCommandsLog
        {
            get { return (StringBuilder) _currentBatchCommandsLogField.GetValue(this); }
            set { _currentBatchCommandsLogField.SetValue(this, value); }
        }
        private int _batchSize
        {
            get { return BatchSize; }
        }
        // ReSharper restore InconsistentNaming

        private SqlClientSqlCommandSet CreateConfiguredBatch()
        {
            return (SqlClientSqlCommandSet)_createConfiguredBatchMethod.Invoke(this, null);
        }
        
        public ReliableSqlClientBatchingBatcher(ConnectionManager connectionManager, IInterceptor interceptor)
            : base(connectionManager, interceptor)
        {
            _connectionManager = connectionManager;
        }
        #endregion

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
            _currentBatch.Append((System.Data.SqlClient.SqlCommand)(ReliableSqlCommand)batchUpdate);
            #region NHibernate code
            if (_currentBatch.CountOfCommands >= _batchSize)
            {
                ExecuteBatchWithTiming(batchUpdate);
            }
            #endregion
        }

        public override Task AddToBatchAsync(IExpectation expectation, CancellationToken cancellationToken)
        {
            #region NHibernate code
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<object>(cancellationToken);
            }
            try
            {
                _totalExpectedRowsAffected += expectation.ExpectedRowCount;
                var batchUpdate = CurrentCommand;
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
                    Log.Debug("Adding to batch:{0}", lineWithParameters);
                }
                #endregion

                _currentBatch.Append((System.Data.SqlClient.SqlCommand)(ReliableSqlCommand)batchUpdate);

                #region NHibernate code
                if (_currentBatch.CountOfCommands >= _batchSize)
                {
                    return ExecuteBatchWithTimingAsync(batchUpdate, cancellationToken);
                }
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException<object>(ex);
            }
            #endregion
        }

        // Need this method call in this class rather than the base class to ensure Prepare is called... if only it was virtual :(
        protected void ExecuteBatch(DbCommand ps)
        {
            try
            {
                Log.Debug("Executing batch");
                CheckReaders();
                Prepare(_currentBatch.BatchCommand);
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

        #region Possible Future Use if async support for retries is added (commented out)
        // protected async Task ExecuteBatchAsync(DbCommand ps, CancellationToken cancellationToken)
        // {
        //     cancellationToken.ThrowIfCancellationRequested();
        //     try
        //     {
        //         Log.Debug("Executing batch");
        //         await (CheckReadersAsync(cancellationToken)).ConfigureAwait(false);
        //         await (PrepareAsync(_currentBatch.BatchCommand, cancellationToken)).ConfigureAwait(false);
        //         if (Factory.Settings.SqlStatementLogger.IsDebugEnabled)
        //         {
        //             Factory.Settings.SqlStatementLogger.LogBatchCommand(_currentBatchCommandsLog.ToString());
        //         }
        //         int rowsAffected;
        //         try
        //         {
        //             rowsAffected = _currentBatch.ExecuteNonQuery();
        //         }
        //         catch (DbException e)
        //         {
        //             throw ADOExceptionHelper.Convert(Factory.SQLExceptionConverter, e, "could not execute batch command.");
        //         }
        //
        //         Expectations.VerifyOutcomeBatched(_totalExpectedRowsAffected, rowsAffected, ps);
        //     }
        //     finally
        //     {
        //         ClearCurrentBatch();
        //     }
        // }
        #endregion
        
        // Copied from NHibernate base class
        private void ClearCurrentBatch()
        {
            _currentBatch.Dispose();
            _totalExpectedRowsAffected = 0;
            _currentBatch = CreateConfiguredBatch();

            if (Factory.Settings.SqlStatementLogger.IsDebugEnabled)
            {
                _currentBatchCommandsLog = new StringBuilder().AppendLine("Batch commands:");
            }
        }

        /// <summary>
        /// Prepares the <see cref="DbCommand"/> for execution in the database.
        /// </summary>
        /// <remarks>
        /// This takes care of hooking the <see cref="DbCommand"/> up to an <see cref="DbConnection"/>
        /// and <see cref="DbTransaction"/> if one exists.  It will call <c>Prepare</c> if the Driver
        /// supports preparing commands.
        /// </remarks>
        private new void Prepare(DbCommand cmd)
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
                        cmd.Connection = (System.Data.SqlClient.SqlConnection) sessionConnection;
                    }
                }
                else
                {
                    cmd.Connection = (System.Data.SqlClient.SqlConnection) sessionConnection;
                }

                _connectionManager.CurrentTransaction?.Enlist(cmd);
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
            var connection = (ReliableSqlDbConnection)_connectionManager.GetConnection();
            ReliableAdoTransaction.ExecuteWithRetry(connection, () => ExecuteBatch(ps));
        }
        
        protected override async Task DoExecuteBatchAsync(DbCommand ps, CancellationToken cancellationToken)
        {
            var connection = (ReliableSqlDbConnection) await _connectionManager.GetConnectionAsync(cancellationToken);
            ReliableAdoTransaction.ExecuteWithRetry(connection, () => ExecuteBatch(ps));
            
            // NOTE: To support full async, changes will need to be made all the way through to Enterprise Library code
            // ReliableAdoTransaction.ExecuteWithRetry(connection, async () => await ExecuteBatchAsync(ps, cancellationToken));
        }
    }
}