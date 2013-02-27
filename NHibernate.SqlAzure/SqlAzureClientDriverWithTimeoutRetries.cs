using System;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.SqlAzure;
using Microsoft.Practices.TransientFaultHandling;

namespace NHibernate.SqlAzure
{
    /// <summary>
    /// NHibernate client driver for SQL Azure that includes the Enterprise Library transient fault-handling as well as timeout retries.
    /// Note: Timeout errors can be caused by unoptimised queries that you mmight be executing as well as being a transient exception.
    /// </summary>
    public class SqlAzureClientDriverWithTimeoutRetries : DefaultReliableSql2008ClientDriver<SqlAzureTransientErrorDetectionStrategyWithTimeouts> {}

    /// <summary>
    /// Transient error detection strategy for SQL Azure that extends the Enterprise Library detection strategy and also detects timeout exceptions.
    /// </summary>
    public class SqlAzureTransientErrorDetectionStrategyWithTimeouts : ITransientErrorDetectionStrategy
    {
        public bool IsTransient(Exception ex)
        {
            if (new SqlAzureTransientErrorDetectionStrategy().IsTransient(ex))
                return true;

            // Timeout exception: error code -2
            // http://social.msdn.microsoft.com/Forums/en-US/ssdsgetstarted/thread/7a50985d-92c2-472f-9464-a6591efec4b3/
            SqlException sqlException;
            return ex != null
                && (sqlException = ex as SqlException) != null
                && sqlException.Errors.Cast<SqlError>().Any(error => error.Number == -2);
        }
    }
}
