using System.Data;
using System.Data.SqlClient;
using System.Threading;
using NHibernate.Driver;
using NHibernate.SqlAzure.Tests.Config;
using NUnit.Framework;

namespace NHibernate.SqlAzure.Tests
{
    [TestFixture]
    class WhenConnectingLocalTestingSqlAzureClientDriverShould : ConnectionTests<LocalTestingReliableSql2008ClientDriver>
    {
        [Test]
        public void Establish_connection_during_temporary_shutdown_of_sql_server()
        {
            TestConnectionEstablishment();
        }
    }

    [TestFixture]
    class WhenConnectingSql2008ClientDriverShould : ConnectionTests<Sql2008ClientDriver>
    {
        [Test]
        [ExpectedException(typeof(SqlException))]
        public void Fail_to_establish_connection_during_temporary_shutdown_of_sql_server()
        {
            TestConnectionEstablishment();
        }
    }

    abstract class ConnectionTests<T> : NonPooledNHibernateTestBase<T>
        where T : SqlClientDriver
    {
        protected void TestConnectionEstablishment()
        {
            using (TemporarilyShutdownSqlServerExpress())
            {
                for (var i = 0; i < 200; i++)
                {
                    using (var session = CreateSession())
                    {
                        Assert.That(session.Connection.State == ConnectionState.Open);
                        Thread.Sleep(1);
                    }
                }
            }
        }
    }
}
