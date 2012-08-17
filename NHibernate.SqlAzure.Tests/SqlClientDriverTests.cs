using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using FizzWare.NBuilder;
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
    class LocalTestingSqlAzureClientDriverShould : SqlClientDriverShould<LocalTestingSqlAzureClientDriver>
    {
        [Test]
        public void Execute_commands_during_temporary_shutdown_of_sql_server()
        {
            using (TemporarilyShutdownSqlServerExpress())
            {
                for (var i = 0; i < 100; i++)
                {
                    Insert_and_select_entity();
                    Thread.Sleep(50);
                }
            }
        }

        [Test]
        public void Establish_connection_during_temporary_shutdown_of_sql_server()
        {
            // todo
        }
    }

    [TestFixture]
    class SqlAzureClientDriverShould : SqlClientDriverShould<SqlAzureClientDriver> {}

    [TestFixture]
    class Sql2008ClientDriverShould : SqlClientDriverShould<Sql2008ClientDriver>
    {
        [Test]
        [ExpectedException(typeof(GenericADOException))]
        public void Fail_to_execute_commands_during_temporary_shutdown_of_sql_server()
        {
            using (TemporarilyShutdownSqlServerExpress())
            {
                for (var i = 0; i < 100; i++)
                {
                    Insert_and_select_entity();
                    Thread.Sleep(50);
                }
            }
        }

        [Test]
        public void Fail_to_establish_connection_during_temporary_shutdown_of_sql_server()
        {
            // todo
        }
    }

    abstract class SqlClientDriverShould<T> : NHibernateTestBase<T> where T : SqlClientDriver
    {
        [Test]
        public void Perform_empty_select()
        {
            var user = Session.Get<User>(-1);

            Assert.That(user, Is.Null);
        }

        [Test]
        public void Insert_and_select_entity()
        {
            var user = new User { Name = "Name" };
            var session = CreateSession();
            session.Save(user);

            var dbUser = Session.Get<User>(user.Id);

            Assert.That(dbUser.Name, Is.EqualTo(user.Name));
        }

        [Test]
        public void Insert_and_select_multiple_entities()
        {
            var users = Builder<User>.CreateListOfSize(100)
                .All().With(u => u.Properties = new List<UserProperty>
                {
                    new UserProperty {Name = "Name", Value = "Value", User = u}
                })
                .Build().OrderBy(u => u.Name).ToList();
            using (var t = Session.BeginTransaction())
            {
                users.ForEach(u => Session.Save(u));
                t.Commit();
            }

            var dbUsers = Session.QueryOver<User>()
                .WhereRestrictionOn(u => u.Id).IsIn(users.Select(u => u.Id).ToArray())
                .OrderBy(u => u.Name).Asc
                .List();

            Assert.That(dbUsers, Has.Count.EqualTo(users.Count));
            for (var i = 0; i < users.Count; i++)
            {
                Assert.That(dbUsers[i], Has.Property("Name").EqualTo(users[i].Name), "User " + i);
                Assert.That(dbUsers[i], Has.Property("Id").EqualTo(users[i].Id), "User " + i);
                var userProperties = dbUsers[i].Properties;
                Assert.That(userProperties, Is.Not.Null, "User " + i + " Properties");
                Assert.That(userProperties, Has.Count.EqualTo(1), "User " + i + " Properties");
                Assert.That(userProperties[0], Has.Property("Name").EqualTo("Name"), "User " + i + " property 0");
                Assert.That(userProperties[0], Has.Property("Value").EqualTo("Value"), "User " + i + " property 0");
            }
        }

        [Test]
        public void Select_a_scalar()
        {
            // todo
        }

        [Test]
        public void Insert_and_update_an_entity()
        {
            // todo
        }

        [Test]
        public void Insert_and_update_multiple_entities()
        {
            // todo
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

                Thread.Sleep(20);
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