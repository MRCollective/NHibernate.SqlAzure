using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.SqlAzure;
using NHibernate.Connection;

namespace NHibernate.SqlAzure
{
    /// <summary>
    /// A ConnectionProvider that uses a SqlAzureClientDriver to create connections.
    /// </summary>
    public class SqlAzureDriverConnectionProvider : DriverConnectionProvider
    {
        /// <summary>
        /// Gets a new open <see cref="IDbConnection"/> through the driver.
        /// </summary>
        /// <remarks>
        /// Returns the underlying <see cref="SqlConnection"/>.
        /// </remarks>
        /// <returns>
        /// An open <see cref="IDbConnection"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// If there are any problem creating or opening the <see cref="IDbConnection"/>.
        /// </exception>
        public override IDbConnection GetConnection()
        {
            return ((ReliableSqlConnection) base.GetConnection()).Current;
        }
    }
}