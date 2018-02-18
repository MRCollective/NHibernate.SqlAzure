﻿using System.Data;
using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.SqlAzure;
using NHibernate.AdoNet;
using NHibernate.Driver;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace NHibernate.SqlAzure
{
	using System.Data.Common;

	/// <summary>
    /// Abstract base class that enables the creation of an NHibernate client driver that extends the Sql 2008 driver,
    /// but adds in transient fault handling retry logic via <see cref="ReliableSqlConnection"/>.
    /// </summary>
    public abstract class ReliableSql2008ClientDriver : Sql2008ClientDriver, IEmbeddedBatcherFactoryProvider
    {
        /// <summary>
        /// Provides a <see cref="ReliableSqlConnection"/> instance to use for connections.
        /// </summary>
        /// <returns>A reliable connection</returns>
        protected abstract ReliableSqlConnection CreateReliableConnection();

        /// <summary>
        /// Creates an uninitialized <see cref="T:System.Data.DbConnection"/> object for the SqlClientDriver.
        /// </summary>
        /// <value>
        /// An unitialized <see cref="T:System.Data.SqlClient.SqlConnection"/> object.
        /// </value>
        public override DbConnection CreateConnection()
        {
            return new ReliableSqlDbConnection(CreateReliableConnection());
        }

        /// <summary>
        /// Creates an uninitialized <see cref="T:System.Data.DbCommand"/> object for the SqlClientDriver.
        /// </summary>
        /// <value>
        /// An unitialized <see cref="T:System.Data.SqlClient.SqlCommand"/> object.
        /// </value>
        public override DbCommand CreateCommand()
        {
            return new ReliableSqlCommand();
        }

        /// <summary>
        /// Returns the class to use for the Batcher Factory.
        /// </summary>
        public System.Type BatcherFactoryClass
        {
            get { return typeof(ReliableSqlClientBatchingBatcherFactory); }
        }
    }
}
