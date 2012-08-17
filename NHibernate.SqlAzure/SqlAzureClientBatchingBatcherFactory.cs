// This file was copied from NHibernate.AdoNet.SqlClientBatchingBatcherFactory, but modified to use SqlAzureClientBatchingBatcher
using NHibernate.AdoNet;
using NHibernate.Engine;

namespace NHibernate.SqlAzure
{
    public class SqlAzureClientBatchingBatcherFactory : IBatcherFactory
    {
        public virtual IBatcher CreateBatcher(ConnectionManager connectionManager, IInterceptor interceptor)
        {
            return new SqlAzureClientBatchingBatcher(connectionManager, interceptor);
        }
    }
}