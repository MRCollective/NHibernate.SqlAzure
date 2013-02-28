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
            if (base.IsTransient(ex))
                return true;

            if (this.IsOurTransient(ex))
                return true;
            
            return false;
        }

        protected virtual bool IsOurTransient(Exception ex)
        {
            if (IsConnectionTimeout(ex))
                return true;

            if (ex.InnerException != null)
                return IsOurTransient(ex.InnerException);
           
            return false;
        }

        protected virtual bool IsConnectionTimeout(Exception ex)
        {
            // Timeout exception: error code -2
            // http://social.msdn.microsoft.com/Forums/en-US/ssdsgetstarted/thread/7a50985d-92c2-472f-9464-a6591efec4b3/
            SqlException sqlException;
            return ex != null
                   && (sqlException = ex as SqlException) != null
                   && sqlException.Errors.Cast<SqlError>().Any(error => error.Number == -2);
        }
    }
}