using System.Configuration;
using System.Reflection;
using System.Transactions;
using NHibernate.Driver;
using NUnit.Framework;

namespace NHibernate.SqlAzure.Tests.Config
{
    public abstract class NHibernateTestBase<T> where T: SqlClientDriver
    {
        private ISessionFactory _sessionFactory;
        protected ISession Session;
        //private TransactionScope _transactionScope;
        protected FluentRunner Migrator;

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            Initialize(ConfigurationManager.ConnectionStrings["Database"].ConnectionString, typeof(NHibernateConfiguration<>).Assembly);
        }

        [SetUp]
        public virtual void Setup()
        {
            //_transactionScope = new TransactionScope();
            Session = _sessionFactory.OpenSession();
        }

        [TearDown]
        public virtual void TearDown()
        {
            //_transactionScope.Dispose();
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
