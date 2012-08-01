using NHibernate.Driver;
using NHibernate.SqlAzure.Tests.Config;
using NHibernate.SqlAzure.Tests.Entities;
using NUnit.Framework;

namespace NHibernate.SqlAzure.Tests
{
    // Run the tests against the standard Sql2008Client driver as well as the SqlAzureClientDriver
    // That way, we know if the test is broken because of the SqlAzureClientDriver or the test is wrong
    [TestFixture]
    class SqlAzureClientDriverShould : SqlClientDriverShould<SqlAzureClientDriver> { }
    [TestFixture]
    class Sql2008ClientDriverShould : SqlClientDriverShould<Sql2008ClientDriver> { }
    
    abstract class SqlClientDriverShould<T> : NHibernateTestBase<T> where T : SqlClientDriver
    {
        [Test]
        public void Perform_simple_select()
        {
            var user = Session.QueryOver<User>().Where(u => u.Id == 1).SingleOrDefault();
            
            Assert.That(user, Is.Null);
        }
    }
}
