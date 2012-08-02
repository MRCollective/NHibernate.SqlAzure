using System;
using System.Data;
using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.SqlAzure;
using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.Tests;
using Microsoft.Practices.TransientFaultHandling;

namespace NHibernate.SqlAzure.Tests.Config
{
    public class TestableSqlAzureClientDriver : SqlAzureClientDriver
    {
        public static int CommandError { get; set; }
        public static int ConnectionError { get; set; }

        public override IDbConnection CreateConnection()
        {
            var connection = (ReliableSqlConnection) base.CreateConnection();

            connection.CommandRetryPolicy.ExecuteAction(() =>
            {
                if (CommandError > 0)
                {
                    CommandError--;
                    throw FakeSqlExceptionGenerator.GenerateFakeSqlException(ThrottlingCondition.ThrottlingErrorNumber);
                }
            });
            connection.CommandRetryPolicy.Retrying += LogRetry();

            connection.ConnectionRetryPolicy.ExecuteAction(() =>
            {
                if (ConnectionError > 0)
                {
                    ConnectionError--;
                    throw FakeSqlExceptionGenerator.GenerateFakeSqlException(ThrottlingCondition.ThrottlingErrorNumber);
                }
            });
            connection.ConnectionRetryPolicy.Retrying += LogRetry();

            return connection;
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
