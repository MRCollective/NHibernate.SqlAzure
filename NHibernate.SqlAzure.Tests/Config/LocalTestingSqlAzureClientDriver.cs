using System;
using System.Data;
using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.SqlAzure;
using Microsoft.Practices.TransientFaultHandling;

namespace NHibernate.SqlAzure.Tests.Config
{
    public class LocalTestingSqlAzureClientDriver : SqlAzureClientDriver
    {
        public static int CommandError { get; set; }
        public static int ConnectionError { get; set; }

        public override IDbConnection CreateConnection()
        {
            var baseConnection = (ReliableSqlDbConnection) base.CreateConnection();

            var connection = new ReliableSqlConnection(null,
                new RetryPolicy<SqlExpressTransientErrorDetectionStrategy>(baseConnection.ReliableConnection.ConnectionRetryPolicy.RetryStrategy),
                new RetryPolicy<SqlExpressTransientErrorDetectionStrategy>(baseConnection.ReliableConnection.CommandRetryPolicy.RetryStrategy)
            );

            connection.CommandRetryPolicy.Retrying += LogRetry();
            connection.ConnectionRetryPolicy.Retrying += LogRetry();
            
            return new ReliableSqlDbConnection(connection);
        }

        private static EventHandler<RetryingEventArgs> LogRetry()
        {
            return (sender, args) =>
            {
                var msg = String.Format("SQLAzureClientDriver Retry - Count:{0}, Delay:{1}, Exception:{2}", args.CurrentRetryCount, args.Delay, args.LastException);
                Console.WriteLine(msg);
            };
        }
    }
}
