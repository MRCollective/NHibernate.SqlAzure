using FluentNHibernate.Automapping;
using FluentNHibernate.Automapping.Alterations;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using HibernatingRhinos.Profiler.Appender.NHibernate;
using NHibernate.Cfg;
using NHibernate.Driver;
using NHibernate.SqlAzure.Tests.Entities;

namespace NHibernate.SqlAzure.Tests.Config
{
    public class NHibernateConfiguration<T> where T : SqlClientDriver
    {
        private readonly string _connectionString;
        private readonly IPersistenceConfigurer _databaseConfig;

        public NHibernateConfiguration(string connectionString, IPersistenceConfigurer databaseConfig = null)
        {
            _connectionString = connectionString;
            _databaseConfig = databaseConfig ?? MsSqlConfiguration.MsSql2008.ConnectionString(_connectionString).Driver<T>();
        }

        public ISessionFactory GetSessionFactory()
        {
            NHibernateProfiler.Initialize();

            var config = Fluently.Configure()
                .Database(_databaseConfig)
                .Mappings(m => m.AutoMappings
                    .Add(AutoMap.AssemblyOf<NHibernateConfiguration<SqlClientDriver>>()
                        .Where(type => type.Namespace != null && type.Namespace.EndsWith("Entities"))
                        .UseOverridesFromAssemblyOf<NHibernateConfiguration<SqlClientDriver>>()
                    )
                )
                // Show SQL so we can see what and when sql is executed by NH
                .ExposeConfiguration(c => c.SetProperty(Environment.BatchSize, "10"))
                // Turn off cache to make sure all calls actually go to the database
                .ExposeConfiguration(c => c.SetProperty(Environment.UseQueryCache, "false"))
                .ExposeConfiguration(c => c.SetProperty(Environment.UseSecondLevelCache, "false"));

            if (typeof(LocalTestingReliableSql2008ClientDriver).IsAssignableFrom(typeof(T)))
                config.ExposeConfiguration(c => c.SetProperty(Environment.TransactionStrategy,
                    typeof(ReliableAdoNetWithDistributedTransactionFactory).AssemblyQualifiedName));


            return config.BuildSessionFactory();
        }
    }

    public class UserPropertyOverride : IAutoMappingOverride<UserProperty>
    {
        public void Override(AutoMapping<UserProperty> mapping)
        {
            mapping.CompositeId().KeyProperty(u => u.Name).KeyReference(u => u.User, "UserId");
        }
    }

    public class UserOverride : IAutoMappingOverride<User>
    {
        public void Override(AutoMapping<User> mapping)
        {
            mapping.HasMany(u => u.Properties).KeyColumn("UserId").Cascade.All();
        }
    }
}