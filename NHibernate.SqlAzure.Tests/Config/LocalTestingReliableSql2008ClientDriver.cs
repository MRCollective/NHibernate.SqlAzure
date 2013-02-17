﻿using System;
using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.SqlAzure;
using Microsoft.Practices.TransientFaultHandling;

namespace NHibernate.SqlAzure.Tests.Config
{
    public class LocalTestingReliableSql2008ClientDriver : ReliableSql2008ClientDriver
    {
        public static int CommandError { get; set; }
        public static int ConnectionError { get; set; }

        protected override ReliableSqlConnection CreateReliableConnection()
        {
            var retryStrategy = new FixedInterval("Incremental Retry Strategy", 10, TimeSpan.FromSeconds(1));

            var connection = new ReliableSqlConnection(null,
                new RetryPolicy<SqlExpressTransientErrorDetectionStrategy>(retryStrategy),
                new RetryPolicy<SqlExpressTransientErrorDetectionStrategy>(retryStrategy)
            );

            connection.CommandRetryPolicy.Retrying += LogRetry("Command");
            connection.ConnectionRetryPolicy.Retrying += LogRetry("Connection");

            return connection;
        }

        private static EventHandler<RetryingEventArgs> LogRetry(string type)
        {
            return (sender, args) =>
            {
                var msg = String.Format("SQLAzureClientDriver {3} Retry - Count:{0}, Delay:{1}, Exception:{2}\r\n\r\n", args.CurrentRetryCount, args.Delay, args.LastException, type);
                Console.WriteLine(msg);
            };
        }
    }
}
