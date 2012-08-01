NHibernate Reliable SQL Azure Driver
====================================

Provides an NHibernate driver that uses the Microsoft Transient Fault Handling library to allow for reliable SQL Azure connections.

Running the tests
-----------------

If you want to contribute to this library then you need to:

1. Load the solution (allow the NuGet package restore to grab all the packages)
2. Compile the solution (.NET 4, AnyCPU)
3. Create a database on your local SQLExpress instance called `NHibernateSqlAzureTests` and grant the user running the NUnit runner `dbowner` access.
    * If you want to use a different database simply change the `Database` ConnectionString in `App.config`
4. Run the `NHibernate.SqlAzure.Tests` project with your NUnit test runner of choice

