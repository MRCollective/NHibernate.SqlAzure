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

        #region SQLExpress shutdown code
        private readonly ServiceController _serviceController = new ServiceController { MachineName = Environment.MachineName, ServiceName = "MSSQL$SQLEXPRESS" };

        [TearDown]
        public void TearDown()
        {
            // Make sure that the service is running before stopping the test
            _serviceController.Refresh();
            if (_serviceController.Status == ServiceControllerStatus.PausePending)
                _serviceController.WaitForStatus(ServiceControllerStatus.Paused);
            if (_serviceController.Status == ServiceControllerStatus.ContinuePending)
                _serviceController.WaitForStatus(ServiceControllerStatus.Running);

            if (_serviceController.Status != ServiceControllerStatus.Running)
            {
                Console.WriteLine("SQLExpress service currently at {0} state; restarting...", _serviceController.Status);
                _serviceController.Continue();
                _serviceController.WaitForStatus(ServiceControllerStatus.Running);
            }
        }

        protected ThreadKiller TemporarilyShutdownSqlServerExpress()
        {
            var t = new Thread(MakeSqlTransient);
            t.Start();
            return new ThreadKiller(t);
        }

        private void MakeSqlTransient()
        {
            try
            {
                while (true)
                {
                    _serviceController.Refresh();
                    if (_serviceController.Status == ServiceControllerStatus.Running)
                        _serviceController.Pause();
                    _serviceController.WaitForStatus(ServiceControllerStatus.Paused);

                    _serviceController.Refresh();
                    _serviceController.Continue();
                    _serviceController.WaitForStatus(ServiceControllerStatus.Running);

                    Thread.Sleep(20);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while making SQL transient: {0}", e);
            }
        }

        protected class ThreadKiller : IDisposable
        {
            private readonly Thread _threadToWaitFor;

            public ThreadKiller(Thread threadToWaitFor)
            {
                _threadToWaitFor = threadToWaitFor;
            }

            public void Dispose()
            {
                _threadToWaitFor.Abort();
            }
        }
        #endregion
    }
}
