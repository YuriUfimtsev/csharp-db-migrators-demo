using FluentMigrator;

namespace DbDemo.Migrations;

[Migration(Versions.CreateCategories, "Таблица категорий")]
public class CreateCategories : Migration
{
    public override void Up() =>
        Create.Table("categories")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("name").AsString(100).NotNullable().Unique();

    public override void Down() =>
        Delete.Table("categories");
}
