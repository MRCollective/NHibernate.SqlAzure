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
            // Is the error an error 25 (Can't connect to instance aka service is stopped)
            var sqlException = ex as SqlException;
            return sqlException != null
                && sqlException.Errors.Cast<SqlError>().Any(error => error.Number == 25);
        }
    }
}
