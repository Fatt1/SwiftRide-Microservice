using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Payment.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PaymentDbContext))]
[Migration("20261017000000_AddWalletTopUpPaymentTransactions")]
public sealed class AddWalletTopUpPaymentTransactions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Type", table: "Payments", type: "character varying(20)",
            maxLength: 20, nullable: false, defaultValue: "TripPayment");

        migrationBuilder.AlterColumn<Guid>(
            name: "TripId", table: "Payments", type: "uuid", nullable: true,
            oldClrType: typeof(Guid), oldType: "uuid");

        migrationBuilder.AlterColumn<Guid>(
            name: "DriverId", table: "Payments", type: "uuid", nullable: true,
            oldClrType: typeof(Guid), oldType: "uuid");

        migrationBuilder.DropIndex(name: "IX_Payments_TripId", table: "Payments");
        migrationBuilder.CreateIndex(
            name: "IX_Payments_TripId", table: "Payments", column: "TripId",
            unique: true, filter: "\"TripId\" IS NOT NULL");

        // Preserve idempotency and payment history for top-ups created before this migration.
        migrationBuilder.Sql("""
            INSERT INTO "Payments" ("Id", "TripId", "RiderId", "DriverId", "Amount", "Currency",
                "Status", "PaymentMethod", "IdempotencyKey", "ProcessedAt", "CreatedAt", "LastModifiedAt", "Type")
            SELECT l."Id", NULL, w."UserId", NULL, l."Amount", w."Currency",
                'Completed', 'Wallet', l."Id", l."CreatedAt", l."CreatedAt", l."CreatedAt", 'WalletTopUp'
            FROM "LedgerEntries" l
            JOIN "Wallets" w ON w."Id" = l."WalletId"
            WHERE l."PaymentId" IS NULL AND l."Description" = 'Wallet top-up';

            UPDATE "LedgerEntries"
            SET "PaymentId" = "Id"
            WHERE "PaymentId" IS NULL AND "Description" = 'Wallet top-up';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DO $$ BEGIN IF EXISTS (SELECT 1 FROM \"Payments\" WHERE \"Type\" = 'WalletTopUp') THEN RAISE EXCEPTION 'Cannot roll back while wallet top-up payments exist'; END IF; END $$;");
        migrationBuilder.DropIndex(name: "IX_Payments_TripId", table: "Payments");
        migrationBuilder.DropColumn(name: "Type", table: "Payments");
        migrationBuilder.AlterColumn<Guid>(
            name: "TripId", table: "Payments", type: "uuid", nullable: false,
            oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
        migrationBuilder.AlterColumn<Guid>(
            name: "DriverId", table: "Payments", type: "uuid", nullable: false,
            oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
        migrationBuilder.CreateIndex(
            name: "IX_Payments_TripId", table: "Payments", column: "TripId", unique: true);
    }
}
