using System;
using System.Configuration;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using NHibernate.Driver;
using NUnit.Framework;

namespace NHibernate.SqlAzure.Tests.Config
{
    public abstract class PooledNHibernateTestBase<T> : NHibernateTestBase<T>
        where T:SqlClientDriver
    {
        protected override string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["PooledDatabase"].ConnectionString; }
        }
    }

    public abstract class NonPooledNHibernateTestBase<T> : NHibernateTestBase<T>
        where T : SqlClientDriver
    {
        protected override string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["NonPooledDatabase"].ConnectionString; }
        }
    }

    public abstract class NHibernateTestBase<T> where T: SqlClientDriver
    {
        private ISessionFactory _sessionFactory;
        protected ISession Session;
        protected FluentRunner Migrator;

        protected abstract string ConnectionString { get; }

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            Migrator = new FluentRunner(ConnectionString, typeof(NHibernateConfiguration<>).Assembly);

            if (_sessionFactory != null)
                return;

            Migrator.MigrateToLatest();

            var nHibernateConfig = new NHibernateConfiguration<T>(ConnectionString);
            _sessionFactory = nHibernateConfig.GetSessionFactory();
        }

        protected ISession CreateSession()
        {
            return _sessionFactory.OpenSession();
        }
    }
}
