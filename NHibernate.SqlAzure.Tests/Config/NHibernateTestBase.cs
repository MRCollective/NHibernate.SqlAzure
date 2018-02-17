using System;
using System.Configuration;
using System.Data.SqlClient;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
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
            CreateTestDatabase();

            Migrator = new FluentRunner(ConnectionString, typeof(NHibernateConfiguration<>).Assembly);

            if (_sessionFactory != null)
                return;

            Migrator.MigrateToLatest();

            var nHibernateConfig = new NHibernateConfiguration<T>(ConnectionString);
            _sessionFactory = nHibernateConfig.GetSessionFactory();
        }

        private void CreateTestDatabase()
        {
            var connectionBuilder = new SqlConnectionStringBuilder(ConnectionString);
            var testDatabaseName = connectionBuilder.InitialCatalog;
            connectionBuilder.InitialCatalog = "master";
            using (var connection = new SqlConnection(connectionBuilder.ToString()))
            {
                connection.Open();
                using (var command = new System.Data.SqlClient.SqlCommand(
                    string.Format("USE master IF NOT EXISTS(select * from sys.databases where name = '{0}') CREATE DATABASE {0}", testDatabaseName), connection
                ))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        protected ISession CreateSession()
        {
            return _sessionFactory.OpenSession();
        }

        #region SQLExpress shutdown code
        private readonly ServiceController _serviceController = new ServiceController { MachineName = Environment.MachineName, ServiceName = ConfigurationManager.AppSettings["SqlServerServiceName"] };

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

        protected CancellableTask TemporarilyShutdownSqlServerExpress()
        {
            var tokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                while (!tokenSource.IsCancellationRequested)
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
            }, tokenSource.Token);
            return new CancellableTask(tokenSource);
        }
        
        protected class CancellableTask : IDisposable
        {
            private readonly CancellationTokenSource _tokenSource;

            public CancellableTask(CancellationTokenSource tokenSource)
            {
                _tokenSource = tokenSource;
            }

            public void Dispose()
            {
                _tokenSource.Cancel();
            }
        }
        #endregion
    }
}
