// This file was copied from NHibernate.AdoNet.SqlClientBatchingBatcherFactory, but modified to use ReliableSqlClientBatchingBatcher
using NHibernate.AdoNet;
using NHibernate.Engine;

namespace NHibernate.SqlAzure
{
    /// <summary>
    /// An <see cref="IBatcherFactory"/> implementation that creates
    /// <see cref="ReliableSqlClientBatchingBatcher"/> instances.
    /// </summary>
    public class ReliableSqlClientBatchingBatcherFactory : IBatcherFactory
    {
        /// <summary>
        /// Creates the batcher.
        /// </summary>
        /// <param name="connectionManager">The connection manager</param>
        /// <param name="interceptor">The interceptor</param>
        /// <returns>The <see cref="ReliableSqlClientBatchingBatcher"/> instance</returns>
        public virtual IBatcher CreateBatcher(ConnectionManager connectionManager, IInterceptor interceptor)
        {
            return new ReliableSqlClientBatchingBatcher(connectionManager, interceptor);
        }
    }
}