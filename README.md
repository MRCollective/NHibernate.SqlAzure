NHibernate Reliable SQL Azure Driver
====================================

Provides an NHibernate driver that uses the Microsoft Transient Fault Handling library to allow for reliable SQL Azure connections.

This library is build against the latest version of NHibernate (3.3.1.4000) so you will need to update to that version to use this library.

Using the provider when using Fluent NHibernate
-----------------------------------------------

To use the provider:

1. `Update-Package FluentNHibernate`
2. `Install-Package NHibernate.SqlAzure`
3. Set the `Database` to use `SqlAzureClientDriver` as the client driver, e.g.:

        Fluently.Configure()
            .Database(MsSqlConfiguration.MsSql2008.ConnectionString(connectionString).Driver<SqlAzureClientDriver>())

Using the provider when using an XML configuration
--------------------------------------------------

To use the provider:

1. `Update-Package NHibernate`
2. `Install-Package NHibernate.SqlAzure`
3. Set the `connection.driver_class` property on the session factory configuration to `NHibernate.SqlAzure.SqlAzureClientDriver, NHibernate.SqlAzure`.

Reliable transactions
---------------------

The Enterprise Library code doesn't seem to provide any rety logic when beginning transactions. This may be because it will rarely be a problem or you might not want to continue the transaction if there was a potential problem starting it. However, in order to get the unit tests for this library to pass, I needed the transaction to be resilient too so I created some classes that allow you to add retry logic when beginning a transaction. This may well be useful to others so I've included it as part of the library. See the next two sections to understand how to make use of this.

Using reliable transactions when using Fluent NHibernate
--------------------------------------------------------

Set the `TransactionStrategy` environment property to use the `ReliableAdoNetWithDistributedTransactionFactory` class:

	config.ExposeConfiguration(c => c.SetProperty(Environment.TransactionStrategy,
		typeof(ReliableAdoNetWithDistributedTransactionFactory).AssemblyQualifiedName));

Using reliable transactions when using an XML configuration
-----------------------------------------------------------

Set the `transaction.factory_class` property on the session factory configuration to `NHibernate.SqlAzure.ReliableAdoNetWithDistributedTransactionFactory, NHibernate.SqlAzure`.

Extending the provider or adding logging for failed attempts or applying different retry strategies / transient error detection strategies
------------------------------------------------------------------------------------------------------------------------------------------

Follow the pattern that the `LocalTestingSqlAzureClientDriver` class uses to extend the `ReliableSql2008ClientDriver` class and provide a different `ReliableSqlConnection` instance with the configuration you want.

Running the tests
-----------------

If you want to contribute to this library then you need to:

1. Load the solution (allow the NuGet package restore to grab all the packages)
2. Compile the solution (.NET 4, AnyCPU)
3. Create a database on your local SQLExpress instance called `NHibernateSqlAzureTests` and grant the user running the NUnit runner `dbowner` access.
    * If you want to use a different database simply change the `Database` ConnectionString in `App.config`, but note: you may also need to change the service name to stop / start in `SqlClientDriverTests.cs`
4. Run the `NHibernate.SqlAzure.Tests` project with your NUnit test runner of choice
    * The user running the tests must have Administrator access on the computer so that the Windows Service for the database can be shutdown and restarted
	* Note: Your database will be taken down and brought back up repeatedly when running the tests so only run them against a development database.

