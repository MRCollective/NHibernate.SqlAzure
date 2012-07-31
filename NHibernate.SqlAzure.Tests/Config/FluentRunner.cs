using System;
using System.Diagnostics;
using System.Reflection;
using FluentMigrator.Runner.Announcers;
using FluentMigrator.Runner.Initialization;

namespace NHibernate.SqlAzure.Tests.Config
{
    public class FluentRunner
    {
        private readonly string _connectionString;
        private readonly Assembly _migrationAssembly;
        private readonly string _database;
        private long _version;
        private string _task;

        public FluentRunner(string connectionString, Assembly migrationAssembly, string database = "sqlserver2008")
        {
            _connectionString = connectionString;
            _migrationAssembly = migrationAssembly;
            _database = database;
        }

        public void MigrateTo(long version)
        {
            _version = version;
            _task = _version == 0 ? "rollback:all" : "rollback:toversion";
            Execute();
        }

        public void MigrateToLatest()
        {
            _task = "migrate:up";
            Execute();
        }

        private void Execute()
        {
            var announcer = new TextWriterAnnouncer(Console.Out) {ShowElapsedTime = true, ShowSql = true};
            var runnerContext = new RunnerContext(announcer)
            {
                Database = _database,
                Task = _task,
                Connection = _connectionString,
                Target = _migrationAssembly.CodeBase.Replace("file:///", ""),
                Version = _version
            };

            Trace.TraceInformation("#\n# Executing migration task {0}...\n#\n", _task);
            var task = new TaskExecutor(runnerContext);
            task.Execute();
            Trace.TraceInformation("\n#\n# Task {0} complete!\n#", _task);
        }
    }
}
