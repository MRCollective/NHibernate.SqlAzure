using System;
using System.Data.SqlClient;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.Practices.TransientFaultHandling;
using NHibernate.Driver;
using NHibernate.Exceptions;
using NHibernate.SqlAzure.RetryStrategies;
using NHibernate.SqlAzure.Tests.Config;
using NUnit.Framework;

namespace NHibernate.SqlAzure.Tests
{
    [TestFixture]
    internal class SqlAzureTransientErrorDetectionStrategyWithTimeoutsShould :
        PooledNHibernateTestBase<Sql2008ClientDriver>
    {
        [Test]
        public void Retry_when_timeout_occurs()
        {
            try
            {
                using (var session = CreateSession())
                {
                    session.CreateSQLQuery(@"WAITFOR DELAY '00:02'").SetTimeout(1).ExecuteUpdate();
                }
            }
            catch (Exception e)
            {
                Assert.That(new SqlAzureTransientErrorDetectionStrategyWithTimeouts().IsTransient(e));
                return;
            }
            Assert.Fail("No timeout exception was thrown!");
        }

        [Test]
        public void Mark_unwrapped_timeout_exception_as_transient()
        {
            var e = SqlExceptionGenerator.GetSqlException(-2);

            Assert.That(new SqlAzureTransientErrorDetectionStrategyWithTimeouts().IsTransient(e));
        }

        [Test]
        public void Mark_wrapped_timeout_exception_as_transient()
        {
            var e = new GenericADOException("Wrapped exception", SqlExceptionGenerator.GetSqlException(-2));

            Assert.That(new SqlAzureTransientErrorDetectionStrategyWithTimeouts().IsTransient(e));
        }

        [Test]
        public void Mark_timeout_exception_as_transient()
        {
            var e = new TimeoutException();

            Assert.That(new SqlAzureTransientErrorDetectionStrategyWithTimeouts().IsTransient(e));
        }
    }

    [TestFixture(typeof(SqlAzureTransientErrorDetectionStrategy))]
    [TestFixture(typeof(SqlAzureTransientErrorDetectionStrategyWithTimeouts))]
    class TransientErrorDetectionStrategyShould<TTransientErrorDetectionStrategy> : PooledNHibernateTestBase<Sql2008ClientDriver>
        where TTransientErrorDetectionStrategy : ITransientErrorDetectionStrategy, new()
    {
        [Test]
        public void Mark_unwrapped_exceptions_as_transient()
        {
            var e = SqlExceptionGenerator.GetSqlException(40197);

            Assert.That(new TTransientErrorDetectionStrategy().IsTransient(e));
        }

        [Test]
        public void Mark_nhibernate_adonet_wrapped_exceptions_as_transient()
        {
            var e = new GenericADOException("Wrapped exception", SqlExceptionGenerator.GetSqlException(40197));

            Assert.That(new TTransientErrorDetectionStrategy().IsTransient(e));
        }

        [Test]
        public void Mark_nhibernate_transaction_wrapped_exceptions_as_transient()
        {
            var e = new TransactionException("Wrapped exception", SqlExceptionGenerator.GetSqlException(40197));

            Assert.That(new TTransientErrorDetectionStrategy().IsTransient(e));
        }

        [Test]
        public void Mark_nhibernate_invalid_operation_exception_wrapped_exceptions_as_transient()
        {
            var e = new InvalidOperationException("Lazy load error", new GenericADOException("Wrapped exception", SqlExceptionGenerator.GetSqlException(40197)));

            Assert.That(new TTransientErrorDetectionStrategy().IsTransient(e));
        }
    }

    internal static class SqlExceptionGenerator
    {
        public static SqlException GetSqlException(int errorCode)
        {
            var collection = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), true);
            var error = (SqlError)Activator.CreateInstance(typeof(SqlError), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { errorCode, (byte)2, (byte)3, "server name", "error message", "proc", 100 }, null);

            typeof(SqlErrorCollection)
                .GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(collection, new object[] { error });

            return typeof(SqlException)
                .GetMethod("CreateException", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(SqlErrorCollection), typeof(string) }, null)
                .Invoke(null, new object[] { collection, "7.0.0" }) as SqlException;
        }
    }
}
