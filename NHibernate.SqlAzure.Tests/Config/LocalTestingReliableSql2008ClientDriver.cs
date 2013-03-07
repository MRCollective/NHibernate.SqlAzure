using System;
using Microsoft.Practices.TransientFaultHandling;

namespace NHibernate.SqlAzure.Tests.Config
{
    public class LocalTestingReliableSql2008ClientDriver : DefaultReliableSql2008ClientDriver<SqlExpressTransientErrorDetectionStrategy>
    {
        protected override EventHandler<RetryingEventArgs> CommandRetryEventHandler()
        {
            return LogRetry("Command");
        }

        protected override EventHandler<RetryingEventArgs> ConnectionRetryEventHandler()
        {
            return LogRetry("Connection");
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
