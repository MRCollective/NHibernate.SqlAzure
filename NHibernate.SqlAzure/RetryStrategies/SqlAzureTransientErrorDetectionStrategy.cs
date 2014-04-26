using System;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;


namespace NHibernate.SqlAzure.RetryStrategies
{
    /// <summary>
    /// Transient error detection strategy for SQL Azure that is a copy of the Enterprise Library detection strategy.
    /// </summary>
    public class SqlAzureTransientErrorDetectionStrategy : ITransientErrorDetectionStrategy
    {
        private readonly SqlDatabaseTransientErrorDetectionStrategy _entLibStrategy = new SqlDatabaseTransientErrorDetectionStrategy();
        public virtual bool IsTransient(Exception ex)
        {
            return IsTransientAzureException(ex);
        }

        private bool IsTransientAzureException(Exception ex)
        {
            if (ex == null)
                return false;

            return _entLibStrategy.IsTransient(ex)
                || IsNewTransientError(ex)
                || IsTransientAzureException(ex.InnerException);
        }

        private bool IsNewTransientError(Exception ex)
        {
            // From Enterprise Library 6 changelog (see https://entlib.codeplex.com/wikipage?title=EntLib6ReleaseNotes):
            // Error code 40540 from SQL Database added as a transient error (see http://msdn.microsoft.com/en-us/library/ff394106.aspx#bkmk_throt_errors).
            // Added error codes 10928 and 10929 from SQL Database as transient errors (see http://blogs.msdn.com/b/psssql/archive/2012/10/31/worker-thread-governance-coming-to-azure-sql-database.aspx).

            SqlException sqlException;
            return (sqlException = ex as SqlException) != null
                   && sqlException.Errors.Cast<SqlError>().Any(error => error.Number == 40540 || error.Number == 10928 || error.Number == 10929);
        }
    }
}
