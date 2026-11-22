using FluentMigrator;

namespace DbDemo.Migrations;

[Migration(Versions.CreateProducts, "Таблица товаров")]
public class CreateProducts : Migration
{
    public override void Up() =>
        Create.Table("products")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("price").AsDecimal(10, 2).NotNullable()
            .WithColumn("category_id").AsInt32().Nullable()
                .ForeignKey("fk_products_categories", "categories", "id")
            .WithColumn("created_at").AsDateTimeOffset().NotNullable()
                .WithDefaultValue(RawSql.Insert("now()"));

    public override void Down() =>
        Delete.Table("products");
}
