using System;
using System.Collections.Generic;
using System.Linq;
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
    // Also, test the retry logic actually fires by using the LocalTestingReliableSql2008ClientDriver that provides
    //  a reliable connection with a local error specific transient error detection strategy
    [TestFixture]
    class LocalTestingSqlAzureClientDriverShould : SqlClientDriverShould<LocalTestingReliableSql2008ClientDriver>
    {
        [Test]
        public void Execute_non_batching_commands_during_temporary_shutdown_of_sql_server()
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
        public void Execute_batching_commands_during_temporary_shutdown_of_sql_server()
        {
            using (TemporarilyShutdownSqlServerExpress())
            {
                for (var i = 0; i < 20; i++)
                {
                    Insert_and_select_multiple_entities();
                    Thread.Sleep(50);
                }
            }
        }
    }

    [TestFixture]
    class SqlAzureClientDriverShould : SqlClientDriverShould<SqlAzureClientDriver> { }

    [TestFixture]
    class SqlAzureClientDriverWithTimeoutRetriesShould : SqlClientDriverShould<SqlAzureClientDriverWithTimeoutRetries> { }

    [TestFixture]
    class Sql2008ClientDriverShould : SqlClientDriverShould<Sql2008ClientDriver>
    {
        [Test]
        [ExpectedException(typeof(ExpectedErrorException))]
        public void Fail_to_execute_non_batching_commands_during_temporary_shutdown_of_sql_server()
        {
            try
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
            catch (GenericADOException e)
            {
                Console.WriteLine(e);
                throw new ExpectedErrorException();
            }
            Assert.Fail("There was no exception when executing non batching commands during temporary shutdown of SQL server, but one was expected.");
        }

        [Test]
        [ExpectedException(typeof(ExpectedErrorException))]
        public void Fail_to_execute_batching_commands_during_temporary_shutdown_of_sql_server()
        {
            try
            {
                using (TemporarilyShutdownSqlServerExpress())
                {
                    for (var i = 0; i < 100; i++)
                    {
                        Insert_and_select_multiple_entities();
                        Thread.Sleep(50);
                    }
                }
            }
            catch (GenericADOException e)
            {
                Console.WriteLine(e);
                throw new ExpectedErrorException();
            }
            catch (TransactionException e)
            {
                Console.WriteLine(e);
                throw new ExpectedErrorException();
            }
        }
        public class ExpectedErrorException : Exception { }
    }

    abstract class SqlClientDriverShould<T> : PooledNHibernateTestBase<T> where T : SqlClientDriver
    {
        [Test]
        public void Perform_empty_select()
        {
            using (var session = CreateSession())
            {
                var user = session.Get<User>(-1);

                Assert.That(user, Is.Null);
            }
        }

        [Test]
        public void Insert_and_select_entity()
        {
            using (var session = CreateSession())
            using (var session2 = CreateSession())
            {
                var user = new User { Name = "Name" };
                session.Save(user);

                var dbUser = session2.Get<User>(user.Id);

                Assert.That(dbUser.Name, Is.EqualTo(user.Name));
            }
        }

        [Test]
        public void Insert_and_select_multiple_entities()
        {
            using (var session = CreateSession())
            using (var session2 = CreateSession())
            {
                var users = Builder<User>.CreateListOfSize(100)
                .All().With(u => u.Properties = new List<UserProperty>
                {
                    new UserProperty {Name = "Name", Value = "Value", User = u}
                })
                .Build().OrderBy(u => u.Name).ToList();
                using (var t = session.BeginTransaction())
                {
                    users.ForEach(u => session.Save(u));
                    t.Commit();
                }

                var dbUsers = session2.QueryOver<User>()
                    .WhereRestrictionOn(u => u.Id).IsIn(users.Select(u => u.Id).ToArray())
                    .OrderBy(u => u.Name).Asc
                    .List();

                Assert.That(dbUsers, Has.Count.EqualTo(users.Count));
                for (var i = 0; i < users.Count; i++)
                {
                    Assert.That(dbUsers[i], Has.Property("Name").EqualTo(users[i].Name), "User " + i);
                    Assert.That(dbUsers[i], Has.Property("Id").EqualTo(users[i].Id), "User " + i);
                    var userProperties = dbUsers[i].Properties.ToList();
                    Assert.That(userProperties, Is.Not.Null, "User " + i + " Properties");
                    Assert.That(userProperties, Has.Count.EqualTo(1), "User " + i + " Properties");
                    Assert.That(userProperties[0], Has.Property("Name").EqualTo("Name"), "User " + i + " property 0");
                    Assert.That(userProperties[0], Has.Property("Value").EqualTo("Value"), "User " + i + " property 0");
                }
            }
        }

        [Test]
        public void Select_a_scalar()
        {
            using (var session = CreateSession())
            using (var session2 = CreateSession())
            {
                var users = Builder<User>.CreateListOfSize(100).Build().ToList();
                using (var t = session.BeginTransaction())
                {
                    users.ForEach(u => session.Save(u));
                    t.Commit();
                }

                var count = session2.QueryOver<User>()
                    .WhereRestrictionOn(x => x.Id)
                        .IsIn(users.Select(x => x.Id).ToArray())
                    .RowCount();

                Assert.That(count, Is.EqualTo(100));
            }
        }

        [Test]
        public void Insert_and_update_an_entity()
        {
            using (var session = CreateSession())
            using (var session2 = CreateSession())
            {
                var user = new User { Name = "Name1" };
                session.Save(user);
                session.Flush();
                user.Name = "Name2";
                session.Flush();

                var userFromDb = session2.Get<User>(user.Id);

                Assert.That(userFromDb.Name, Is.EqualTo("Name2"));
            }
        }

        [Test]
        public void Insert_and_update_multiple_entities()
        {
            using (var session = CreateSession())
            using (var session2 = CreateSession())
            {
                var users = Builder<User>.CreateListOfSize(100).Build().ToList();
                using (var t = session.BeginTransaction())
                {
                    users.ForEach(u => session.Save(u));
                    t.Commit();
                }
                foreach (var u in users)
                {
                    u.Name += "_2_";
                }
                session.Flush();

                var dbUsers = session2.QueryOver<User>()
                    .WhereRestrictionOn(u => u.Id).IsIn(users.Select(u => u.Id).ToArray())
                    .OrderBy(u => u.Name).Asc
                    .List();

                Assert.That(dbUsers, Has.Count.EqualTo(users.Count));
                foreach (var u in dbUsers)
                {
                    Assert.That(u.Name, Is.StringEnding("_2_"));
                }
            }
        }
    }
}