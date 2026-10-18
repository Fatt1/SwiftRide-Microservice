using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Payment.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PaymentDbContext))]
[Migration("20261018000000_MovePaymentDetailsToLedger")]
// Historical migration: keep this ID because it has already been applied to databases.
public sealed class MovePaymentDetailsToLedger : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LedgerPaymentDetails",
            columns: table => new
            {
                PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                DriverId = table.Column<Guid>(type: "uuid", nullable: true),
                Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                PaymentMethod = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                GatewayToken = table.Column<string>(type: "text", nullable: true),
                GatewayResponse = table.Column<string>(type: "jsonb", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LedgerPaymentDetails", x => x.PaymentId);
                table.ForeignKey("FK_LedgerPaymentDetails_Payments_PaymentId", x => x.PaymentId,
                    "Payments", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.Sql("""
            INSERT INTO "LedgerPaymentDetails" ("PaymentId", "UserId", "DriverId", "Amount",
                "Currency", "PaymentMethod", "GatewayToken", "GatewayResponse")
            SELECT "Id", "RiderId", "DriverId", "Amount", "Currency", "PaymentMethod",
                "GatewayToken", "GatewayResponse"
            FROM "Payments";
            """);

        migrationBuilder.DropColumn(name: "RiderId", table: "Payments");
        migrationBuilder.DropColumn(name: "DriverId", table: "Payments");
        migrationBuilder.DropColumn(name: "Amount", table: "Payments");
        migrationBuilder.DropColumn(name: "Currency", table: "Payments");
        migrationBuilder.DropColumn(name: "PaymentMethod", table: "Payments");
        migrationBuilder.DropColumn(name: "GatewayToken", table: "Payments");
        migrationBuilder.DropColumn(name: "GatewayResponse", table: "Payments");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "RiderId", table: "Payments", type: "uuid", nullable: false,
            defaultValue: Guid.Empty);
        migrationBuilder.AddColumn<Guid>(name: "DriverId", table: "Payments", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "Amount", table: "Payments", type: "numeric(18,2)",
            precision: 18, scale: 2, nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<string>(name: "Currency", table: "Payments", type: "character varying(3)",
            maxLength: 3, nullable: false, defaultValue: "VND");
        migrationBuilder.AddColumn<string>(name: "PaymentMethod", table: "Payments", type: "character varying(10)",
            maxLength: 10, nullable: false, defaultValue: "Wallet");
        migrationBuilder.AddColumn<string>(name: "GatewayToken", table: "Payments", type: "text", nullable: true);
        migrationBuilder.AddColumn<string>(name: "GatewayResponse", table: "Payments", type: "jsonb", nullable: true);

        migrationBuilder.Sql("""
            UPDATE "Payments" p SET "RiderId" = d."UserId", "DriverId" = d."DriverId",
                "Amount" = d."Amount", "Currency" = d."Currency", "PaymentMethod" = d."PaymentMethod",
                "GatewayToken" = d."GatewayToken", "GatewayResponse" = d."GatewayResponse"
            FROM "LedgerPaymentDetails" d WHERE d."PaymentId" = p."Id";
            """);
        migrationBuilder.DropTable(name: "LedgerPaymentDetails");
    }
}
