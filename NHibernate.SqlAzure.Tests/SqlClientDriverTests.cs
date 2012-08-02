using NHibernate.Driver;
using NHibernate.Exceptions;
using NHibernate.SqlAzure.Tests.Config;
using NHibernate.SqlAzure.Tests.Entities;
using NUnit.Framework;

namespace NHibernate.SqlAzure.Tests
{
    // Run the tests against the standard Sql2008Client driver as well as the SqlAzureClientDriver
    // That way, we know if the test is broken because of the SqlAzureClientDriver or the test is wrong
    [TestFixture]
    class SqlAzureClientDriverShould : SqlClientDriverShould<TestableSqlAzureClientDriver>
    {
        [SetUp]
        public override void Setup()
        {
            TestableSqlAzureClientDriver.CommandError = 0;
            TestableSqlAzureClientDriver.ConnectionError = 0;

            base.Setup();
        }
        
        [Test]
        public virtual void Recover_from_temporary_shutdown_of_sql_server_when_opening_connection()
        {
            TestableSqlAzureClientDriver.ConnectionError = 1;
            Session.Close();

            CreateSession().Get<User>(1);

            Assert.That(TestableSqlAzureClientDriver.ConnectionError, Is.EqualTo(0), "The SQLException wasn't thrown so this test didn't test anything");
        }

        [Test]
        public virtual void Recover_from_temporary_shutdown_of_sql_server_when_querying()
        {
            TestableSqlAzureClientDriver.CommandError = 1;

            Perform_simple_select();

            Assert.That(TestableSqlAzureClientDriver.CommandError, Is.EqualTo(0), "The SQLException wasn't thrown so this test didn't test anything");
        }

        [Test]
        [ExpectedException(typeof(GenericADOException))]
        public void Fail_if_the_retry_strategy_is_overwhelmed()
        {
            TestableSqlAzureClientDriver.CommandError = 11;

            Perform_simple_select();
        }
    }

    [TestFixture]
    class Sql2008ClientDriverShould : SqlClientDriverShould<Sql2008ClientDriver> {}
    
    abstract class SqlClientDriverShould<T> : NHibernateTestBase<T> where T : SqlClientDriver
    {
        [Test]
        public void Perform_empty_select()
        {
            var user = Session.Get<User>(1); ;

            Assert.That(user, Is.Null);
        }

        [Test]
        public void Perform_simple_select()
        {
            var user = new User {Name = "Name"};
            var session = CreateSession();
            session.Save(user);
            session.Evict(user);
            session.Flush();
            
            var dbUser = Session.Get<User>(user.Id);

            Assert.That(dbUser.Name, Is.EqualTo(user.Name));
        }
    }
}
