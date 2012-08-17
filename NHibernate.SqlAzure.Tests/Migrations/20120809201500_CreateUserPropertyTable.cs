using FluentMigrator;

namespace NHibernate.SqlAzure.Tests.Migrations
{
    [Migration(20120809201500)]
    public class CreateUserPropertyTable : Migration
    {
        public override void Up()
        {
            Create.Table("UserProperty")
                .WithColumn("UserId").AsInt32().PrimaryKey()
                .WithColumn("Name").AsString(255).PrimaryKey()
                .WithColumn("Value").AsString(255);
        }

        public override void Down()
        {
            Delete.Table("User");
        }
    }
}
