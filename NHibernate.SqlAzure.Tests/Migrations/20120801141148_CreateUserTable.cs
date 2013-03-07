using FluentMigrator;

namespace NHibernate.SqlAzure.Tests.Migrations
{
    [Migration(20120801141148)]
    public class CreateUserTable : Migration
    {
        public override void Up()
        {
            Create.Table("User")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Name").AsString(255).NotNullable();
        }

        public override void Down()
        {
            Delete.Table("User");
        }
    }
}
