using NHibernate.SqlAzure.Tests.Config;
using NHibernate.SqlAzure.Tests.Entities;
using NUnit.Framework;

namespace NHibernate.SqlAzure.Tests
{
    [TestFixture]
    class SqlAzureDriverShould : NHibernateTestBase
    {
        [Test]
        public void Perform_simple_select()
        {
            var user = Session.QueryOver<User>().Where(u => u.Id == 1).SingleOrDefault();
            
            Assert.That(user, Is.Null);
        }
    }
}
