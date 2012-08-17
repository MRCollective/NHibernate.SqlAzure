using System.Configuration;
using System.Reflection;
using NHibernate.Driver;
using NUnit.Framework;

namespace NHibernate.SqlAzure.Tests.Config
{
    public abstract class NHibernateTestBase<T> where T: SqlClientDriver
    {
        private ISessionFactory _sessionFactory;
        protected ISession Session;
        protected FluentRunner Migrator;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            Initialize(ConfigurationManager.ConnectionStrings["Database"].ConnectionString, typeof(NHibernateConfiguration<>).Assembly);
        }

        [SetUp]
        public virtual void Setup()
        {
            Session = _sessionFactory.OpenSession();
        }

        private void Initialize(string connectionString, Assembly migrationAssembly)
        {
            Migrator = new FluentRunner(connectionString, migrationAssembly);

            if (_sessionFactory != null)
                return;

            Migrator.MigrateToLatest();

            var nHibernateConfig = new NHibernateConfiguration<T>(connectionString);
            _sessionFactory = nHibernateConfig.GetSessionFactory();
        }

        protected ISession CreateSession()
        {
            return _sessionFactory.OpenSession();
        }
    }
}
