using FluentMigrator;

namespace DbDemo.Migrations;

[Migration(Versions.AddProductRating, "Рейтинг товара")]
public class AddProductRating : Migration
{
    public override void Up() =>
        Alter.Table("products")
            .AddColumn("rating").AsInt32()
            .NotNullable().WithDefaultValue(0);

    public override void Down() =>
        Delete.Column("rating").FromTable("products");
}
