using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Payment.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PaymentDbContext))]
[Migration("20261012000000_AllowWalletTopUpLedgerEntries")]
public sealed class AllowWalletTopUpLedgerEntries : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<Guid>(
            name: "PaymentId",
            table: "LedgerEntries",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DO $$ BEGIN IF EXISTS (SELECT 1 FROM \"LedgerEntries\" WHERE \"PaymentId\" IS NULL) THEN RAISE EXCEPTION 'Cannot roll back while wallet top-up ledger entries exist'; END IF; END $$;");
        migrationBuilder.AlterColumn<Guid>(
            name: "PaymentId",
            table: "LedgerEntries",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);
    }
}
