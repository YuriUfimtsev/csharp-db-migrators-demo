using FluentMigrator;

namespace DbDemo.Migrations;

/// <summary>Счета для демонстрации транзакций и уровней изоляции.</summary>
[Migration(Versions.CreateAccounts, "Счета")]
public class CreateAccounts : Migration
{
    public override void Up()
    {
        Create.Table("accounts")
            .WithColumn("id").AsInt32().PrimaryKey().Identity()
            .WithColumn("owner").AsString(100).NotNullable()
            .WithColumn("balance").AsDecimal(12, 2).NotNullable();

        Insert.IntoTable("accounts")
            .Row(new { owner = "Анна", balance = 100m })
            .Row(new { owner = "Борис", balance = 50m })
            .Row(new { owner = "Вера", balance = 200m });
    }

    public override void Down() =>
        Delete.Table("accounts");
}
