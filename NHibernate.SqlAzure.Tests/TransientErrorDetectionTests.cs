using System;
using NHibernate.Driver;
using NHibernate.SqlAzure.RetryStrategies;
using NHibernate.SqlAzure.Tests.Config;
using NUnit.Framework;

namespace NHibernate.SqlAzure.Tests
{
    [TestFixture]
    class SqlAzureTransientErrorDetectionStrategyWithTimeoutsShould : PooledNHibernateTestBase<Sql2008ClientDriver>
    {
        [Test]
        public void Retry_when_timeout_occurs()
        {
            try
            {
                using (var session = CreateSession())
                {
                    session.CreateSQLQuery(@"WAITFOR DELAY '00:02'").SetTimeout(1).ExecuteUpdate();
                }
            }
            catch (Exception e)
            {
                Assert.That(new SqlAzureTransientErrorDetectionStrategyWithTimeouts().IsTransient(e));
                return;
            }
            Assert.Fail("No timeout exception was thrown!");
        }
    }
}
