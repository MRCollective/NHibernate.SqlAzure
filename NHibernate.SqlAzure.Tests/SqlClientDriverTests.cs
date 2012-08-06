using System;
using System.ServiceProcess;
using System.Threading;
using NHibernate.Driver;
using NHibernate.Exceptions;
using NHibernate.SqlAzure.Tests.Config;
using NHibernate.SqlAzure.Tests.Entities;
using NUnit.Framework;

namespace NHibernate.SqlAzure.Tests
{
    // Run the tests against the standard Sql2008Client driver as well as the SqlAzureClientDriver
    // That way, we know if the test is broken because of the SqlAzureClientDriver or the test is wrong
    // Also, test the retry logic actually fires by using the LocalTestingSqlAzureClientDriver that provides
    //  a reliable connection with a local error specific transient error detection strategy
    [TestFixture]
    class SqlAzureClientDriverShould : SqlClientDriverShould<TestableSqlAzureClientDriver>
    {
        [Test]
        public virtual void Recover_from_temporary_shutdown_of_sql_server()
        {
            using (TemporarilyShutdownSqlServerExpress())
            {
                for (var i = 0; i < 100; i++)
                {
                    Perform_simple_select();
                    Thread.Sleep(50);
                }
            }
        }
    }



    [TestFixture]
    class Sql2008ClientDriverShould : SqlClientDriverShould<Sql2008ClientDriver>
    {
        [Test]
        [ExpectedException(typeof(GenericADOException))]
        public virtual void Fail_with_temporary_shutdown_of_sql_server()
        {
            using (TemporarilyShutdownSqlServerExpress())
            {
                for (var i = 0; i < 100; i++)
                {
                    Perform_simple_select();
                    Thread.Sleep(50);
                }
            }
        }
    }

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
            var user = new User { Name = "Name" };
            var session = CreateSession();
            session.Save(user);
            session.Evict(user);
            session.Flush();

            var dbUser = Session.Get<User>(user.Id);

            Assert.That(dbUser.Name, Is.EqualTo(user.Name));
        }

        #region SQLExpress shutdown code
        private readonly ServiceController _serviceController = new ServiceController { MachineName = Environment.MachineName, ServiceName = "MSSQL$SQLEXPRESS" };

        [TearDown]
        public override void TearDown()
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

            base.Setup();
        }

        protected ThreadKiller TemporarilyShutdownSqlServerExpress()
        {
            var t = new Thread(MakeSqlTransient);
            t.Start();
            return new ThreadKiller(t);
        }

        private void MakeSqlTransient()
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
                Console.WriteLine("Killing thread: {0}", DateTime.Now.ToString("ss.f"));
                _threadToWaitFor.Abort();
            }
        }
        #endregion
    }
}