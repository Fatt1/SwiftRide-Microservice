using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payment.Infrastructure.Persistence.Migrations;

public partial class RemoveWalletUserRole : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "UserRole", table: "Wallets");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Original roles cannot be recovered after the column is dropped.
        migrationBuilder.AddColumn<string>(
            name: "UserRole", table: "Wallets", type: "character varying(10)",
            maxLength: 10, nullable: false, defaultValue: "");
    }
}
