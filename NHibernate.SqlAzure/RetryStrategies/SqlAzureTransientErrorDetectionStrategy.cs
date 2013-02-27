using System;
using Microsoft.Practices.TransientFaultHandling;
using EntLibSqlAzureTransientErrorDetectionStrategy = Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.SqlAzure.SqlAzureTransientErrorDetectionStrategy;

namespace NHibernate.SqlAzure.RetryStrategies
{
    /// <summary>
    /// Transient error detection strategy for SQL Azure that is a copy of the Enterprise Library detection strategy.
    /// </summary>
    public class SqlAzureTransientErrorDetectionStrategy : ITransientErrorDetectionStrategy
    {
        public virtual bool IsTransient(Exception ex)
        {
            return new EntLibSqlAzureTransientErrorDetectionStrategy().IsTransient(ex);
        }
    }
}
