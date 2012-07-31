using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate.Driver;

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
            var config = Fluently.Configure()
                .Database(_databaseConfig)
                .Mappings(m => m.AutoMappings
                    .Add(AutoMap.AssemblyOf<NHibernateConfiguration<SqlClientDriver>>()
                        .Where(type => type.Namespace != null && type.Namespace.EndsWith("Entities"))
                        .UseOverridesFromAssemblyOf<NHibernateConfiguration<SqlClientDriver>>()
                    )
                )
                .ExposeConfiguration(c => c.SetProperty("hbm2ddl.keywords", "none"));

            return config.BuildSessionFactory();
        }
    }
}