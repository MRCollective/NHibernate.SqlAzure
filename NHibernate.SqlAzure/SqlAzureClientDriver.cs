using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling;
using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.SqlAzure;
using Microsoft.Practices.TransientFaultHandling;
using NHibernate.AdoNet;
using NHibernate.Driver;

namespace NHibernate.SqlAzure
{
    /// <summary>
    /// NHibernate client driver for SQL Azure that extends the Sql 2008 driver, but adds in transient fault handling retry logic.
    /// </summary>
    public class SqlAzureClientDriver : Sql2008ClientDriver, IEmbeddedBatcherFactoryProvider
    {
        /// <summary>
        /// Creates an uninitialized <see cref="T:System.Data.IDbConnection"/> object for the SqlClientDriver.
        /// </summary>
        /// <value>
        /// An unitialized <see cref="T:System.Data.SqlClient.SqlConnection"/> object.
        /// </value>
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

            var connection = new ReliableSqlConnection(null, retryManager.GetDefaultSqlConnectionRetryPolicy(), retryManager.GetDefaultSqlCommandRetryPolicy());
            return new ReliableSqlDbConnection(connection);
        }

        /// <summary>
        /// Creates an uninitialized <see cref="T:System.Data.IDbCommand"/> object for the SqlClientDriver.
        /// </summary>
        /// <value>
        /// An unitialized <see cref="T:System.Data.SqlClient.SqlCommand"/> object.
        /// </value>
        public override IDbCommand CreateCommand()
        {
            return new ReliableSqlCommand();
        }

        /// <summary>
        /// Returns the class to use for the Batcher Factory.
        /// </summary>
        public System.Type BatcherFactoryClass
        {
            get { return typeof(SqlAzureClientBatchingBatcherFactory); }
        }
    }
}