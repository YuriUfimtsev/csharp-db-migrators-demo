using FluentMigrator;

namespace DbDemo.Migrations;

[Migration(Versions.SeedData, "Начальные данные")]
public class SeedData : Migration
{
    public override void Up()
    {
        Insert.IntoTable("categories")
            .Row(new { name = "Периферия" })
            .Row(new { name = "Мониторы" })
            .Row(new { name = "Хранение данных" });

        Execute.Sql("""
            INSERT INTO products (name, price, category_id) VALUES
              ('Мышь',        990,   (SELECT id FROM categories WHERE name = 'Периферия')),
              ('Клавиатура',  2490,  (SELECT id FROM categories WHERE name = 'Периферия')),
              ('Монитор 27"', 21990, (SELECT id FROM categories WHERE name = 'Мониторы')),
              ('SSD 1 ТБ',    6990,  (SELECT id FROM categories WHERE name = 'Хранение данных')),
              ('Флешка 64 ГБ', 590,  (SELECT id FROM categories WHERE name = 'Хранение данных'));
            """);
    }

    public override void Down() =>
        Execute.Sql("DELETE FROM products; DELETE FROM categories;");
}
