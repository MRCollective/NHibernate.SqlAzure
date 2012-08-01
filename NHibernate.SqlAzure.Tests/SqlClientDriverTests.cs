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
    [TestFixture]
    class SqlAzureClientDriverShould : SqlClientDriverShould<SqlAzureClientDriver>
    {
        [Test]
        public virtual void Recover_from_temporary_shutdown_of_sql_server()
        {
            using (TemporarilyShutdownSqlServerExpress())
            {
                Perform_simple_select();
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
                Perform_simple_select();
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
            var user = new User {Name = "Name"};
            var session = CreateSession();
            session.Save(user);
            session.Evict(user);
            session.Flush();
            
            var dbUser = Session.Get<User>(user.Id);

            Assert.That(dbUser.Name, Is.EqualTo(user.Name));
        }

        #region SQLExpress shutdown code
        private readonly ServiceController _serviceController = new ServiceController { MachineName = Environment.MachineName, ServiceName = "MSSQL$SQLEXPRESS" };

        [SetUp]
        public override void Setup()
        {
            // Make sure that the service is running before running the test
            _serviceController.Refresh();
            if (_serviceController.Status != ServiceControllerStatus.Running)
            {
                Console.WriteLine("SQLExpress service currently at {0} state; restarting...", _serviceController.Status);
                _serviceController.Start();
                _serviceController.WaitForStatus(ServiceControllerStatus.Running);
            }

            base.Setup();
        }

        protected ThreadWaiter TemporarilyShutdownSqlServerExpress()
        {
            _serviceController.Refresh();
            if (_serviceController.Status == ServiceControllerStatus.Running)
            {
                Console.WriteLine("Stopping SQLExpress service: {0}", DateTime.Now.ToString("ss.f"));
                _serviceController.Stop();
            }

            _serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
            Console.WriteLine("SQLExpress stopped: {0}", DateTime.Now.ToString("ss.f"));

            var t = new Thread(StartSqlServerExpressAfterAnInterval);
            t.Start();
            return new ThreadWaiter(t);
        }

        private void StartSqlServerExpressAfterAnInterval()
        {
            // The commands seem to time out after 15s
            Console.WriteLine("Sleeping for 20s: {0}", DateTime.Now.ToString("ss.f"));
            Thread.Sleep(TimeSpan.FromSeconds(20));
            Console.WriteLine("Finished sleeping; restarting server: {0}", DateTime.Now.ToString("ss.f"));
            _serviceController.Refresh();
            _serviceController.Start();
            _serviceController.WaitForStatus(ServiceControllerStatus.Running);
            Console.WriteLine("Server restarted: {0}", DateTime.Now.ToString("ss.f"));
        }

        protected class ThreadWaiter : IDisposable
        {
            private readonly Thread _threadToWaitFor;

            public ThreadWaiter(Thread threadToWaitFor)
            {
                _threadToWaitFor = threadToWaitFor;
            }

            public void Dispose()
            {
                Console.WriteLine("Waiting for thread to finish: {0}", DateTime.Now.ToString("ss.f"));
                _threadToWaitFor.Join();
            }
        }
        #endregion
    }
}
