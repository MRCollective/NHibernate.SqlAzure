NHibernate Reliable SQL Azure Driver
====================================

Provides an NHibernate driver that uses the Microsoft Transient Fault Handling library to allow for reliable SQL Azure connections.

This library is build against version 3.3.1.4000 of NHibernate so you will need to update to that version or later to use this library.

Version 2 of this library targets the new version (6) of the Enterprise Library code and as such requires .NET 4.5. If you have a .NET 4.0 application then feel free to use the [latest version in the 1.0 range of this library](https://www.nuget.org/packages/NHibernate.SqlAzure/1.0.0.37).

Using the provider when using Fluent NHibernate
-----------------------------------------------

To use the provider:

1. `Update-Package FluentNHibernate`
2. `Install-Package NHibernate.SqlAzure`
	* or if you want the version that isn't IL-merged with the Microsoft Transient Fault Handling library then `Install-Package NHibernate.SqlAzure.Standalone` (note: that will add 2 other dependencies as well)
3. Set the `Database` to use `SqlAzureClientDriver` as the client driver (note: if you get Timeout exceptions then see the Advanced section below), e.g.:

        Fluently.Configure()
            .Database(MsSqlConfiguration.MsSql2008.ConnectionString(connectionString).Driver<SqlAzureClientDriver>())

Using the provider when using an XML configuration
--------------------------------------------------

To use the provider:

1. `Update-Package NHibernate`
2. `Install-Package NHibernate.SqlAzure`
	* or if you want the version that isn't IL-merged with the Microsoft Transient Fault Handling library then `Install-Package NHibernate.SqlAzure.Standalone` (note: that will add 2 other dependencies as well)
3. Set the `connection.driver_class` property on the session factory configuration to `NHibernate.SqlAzure.SqlAzureClientDriver, NHibernate.SqlAzure`  (note: if you get Timeout exceptions then see the Advanced section below).

Reliable transactions
---------------------

The Enterprise Library code doesn't seem to provide any rety logic when beginning transactions. This may be because it will rarely be a problem or you might not want to continue the transaction if there was a potential problem starting it. However, in order to get the unit tests for this library to pass, I needed the transaction to be resilient too so I created some classes that allow you to add retry logic when beginning a transaction. This may well be useful to others so we've included it as part of the library. See the next two sections to understand how to make use of this.

Using reliable transactions when using Fluent NHibernate
--------------------------------------------------------

Set the `TransactionStrategy` environment property to use the `ReliableAdoNetWithDistributedTransactionFactory` class:

	config.ExposeConfiguration(c => c.SetProperty(Environment.TransactionStrategy,
		typeof(ReliableAdoNetWithDistributedTransactionFactory).AssemblyQualifiedName));

Using reliable transactions when using an XML configuration
-----------------------------------------------------------

Set the `transaction.factory_class` property on the session factory configuration to `NHibernate.SqlAzure.ReliableAdoNetWithDistributedTransactionFactory, NHibernate.SqlAzure`.

Advanced Usage: Extending the provider, add logging for failed attempts or apply different retry strategies / transient error detection strategies
----------------------------------------------------------------------------------------------------------------------------------

It's possible for Timeout exceptions to be both a [transient error caused by Azure and a legitimate timeout caused by unoptimised queries](http://social.msdn.microsoft.com/Forums/en-US/ssdsgetstarted/thread/7a50985d-92c2-472f-9464-a6591efec4b3/) so we've included a transient error detection strategy that detects these timeout exceptions as a transient error and retries. To use it simply change your driver from `SqlAzureClientDriver` to `SqlAzureClientDriverWithTimeoutRetries`. There are a few things to note:

* We recommend you try the `SqlAzureClientDriver` first and then add the one that detects timeouts as transient errors only after you experience timeout errors that you are sure are caused by SQL Azure and not your code
* If the timeout happened in the first place it means that the user's request has already taken a long time so applying a retry policy to that query will make it take even longer (and if the retries also timeout then the page request might even time out (for a web application)).
* If you want visibility of retries then see below for the instructions about how to log retries

There are two abstract base driver classes that you can extend to get more control over the retry policies and use to hook in logging of retries:

* `ReliableSql2008ClientDriver`: Takes care of wrapping the internals of NHibernate to use a `ReliableSqlConnection` rather than a `SqlConnection`. You simply need to override the `CreateReliableConnection` method and instantiate your own `ReliableSqlConnection` in any way you like
* `DefaultReliableSql2008ClientDriver<TTransientErrorDetectionStrategy>`:
	* Defines a connection and command retry policy (based on the example ones used in the [Transient Fault Handling documentation](http://msdn.microsoft.com/en-us/library/hh680900.aspx)
	* Includes overridable methods to return event handlers for logging when any retries occur (`RetryEventHandler`) or alternatively logging when a specific type of retry occurs (`CommandRetryEventHandler` and `ConnectionRetryEventHandler`)
	* Allows you to define what transient error detection strategy you want to use (`TTransientErrorDetectionStrategy`); there are two included in this library that you can use and / or extend (and of course you can always create a completely custom one by extending `ITransientErrorDetectionStrategy`; for an example check out `NHibernate.SqlAzure.Tests.Config.SqlExpressTransientErrorDetectionStrategy` in the test project of the source code):
		* `NHibernate.SqlAzure.RetryStrategies.SqlAzureTransientErrorDetectionStrategy`: A clone of the error detection strategy that comes with the Transient Faut Handling library (except it's not sealed and the `IsTransient` method is virtual so you can extend it
		* `NHibernate.SqlAzure.RetryStrategies.SqlAzureTransientErrorDetectionStrategyWithTimeouts`: The same as above with the addition of detecting timeout exceptions as a transient error; use this with caution as per above

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
