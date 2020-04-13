using NHibernate.SqlAzure.RetryStrategies;

namespace NHibernate.SqlAzure
{
    /// <summary>
    /// NHibernate client driver for SQL Azure that includes the Enterprise Library transient fault-handling as well as timeout retries.
    /// Note: Timeout errors can be caused by unoptimised queries that you might be executing as well as being a transient exception.
    /// </summary>
    public class SqlAzureClientDriverWithTimeoutRetries : DefaultReliableSql2008ClientDriver<SqlAzureTransientErrorDetectionStrategyWithTimeouts> {}
}
