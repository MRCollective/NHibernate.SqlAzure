using System;
using System.Data.SqlClient;
using System.Linq;

namespace NHibernate.SqlAzure.RetryStrategies
{
    /// <summary>
    /// Transient error detection strategy for SQL Azure that extends the Enterprise Library detection strategy and also detects timeout exceptions.
    /// </summary>
    public class SqlAzureTransientErrorDetectionStrategyWithTimeouts : SqlAzureTransientErrorDetectionStrategy
    {
        public override bool IsTransient(Exception ex)
        {
            return base.IsTransient(ex) || IsTransientTimeout(ex);
        }

        protected virtual bool IsTransientTimeout(Exception ex)
        {
            if (IsConnectionTimeout(ex))
                return true;

            return ex.InnerException != null && IsTransientTimeout(ex.InnerException);
        }

        protected virtual bool IsConnectionTimeout(Exception ex)
        {
            // Timeout exception: error code -2
            // http://social.msdn.microsoft.com/Forums/en-US/ssdsgetstarted/thread/7a50985d-92c2-472f-9464-a6591efec4b3/

            // Timeout exception: error code 121
            // http://social.msdn.microsoft.com/Forums/nl-NL/ssdsgetstarted/thread/5e195f94-d4d2-4c2d-8a4e-7d66b4761510

            SqlException sqlException;
            return ex != null
                   && (sqlException = ex as SqlException) != null
                   && sqlException.Errors.Cast<SqlError>().Any(error => error.Number == -2 || error.Number == 121);
        }
    }
}