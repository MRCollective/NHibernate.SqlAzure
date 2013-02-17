using System;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.Practices.TransientFaultHandling;

namespace NHibernate.SqlAzure.Tests.Config
{
    public class SqlExpressTransientErrorDetectionStrategy : ITransientErrorDetectionStrategy
    {
        public bool IsTransient(Exception ex)
        {
            if (ex is TransactionException)
                ex = ex.InnerException;

            // Is the error an error 17142 - The service is paused
            // Is the error an error 233 - Connection error when the process isn't responding
            var sqlException = ex as SqlException;
            return sqlException != null
                && sqlException.Errors.Cast<SqlError>().Any(error => error.Number == 17142 || error.Number == 233);
        }
    }
}
