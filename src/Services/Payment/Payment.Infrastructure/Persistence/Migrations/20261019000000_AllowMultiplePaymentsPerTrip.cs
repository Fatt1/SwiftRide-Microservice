using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Payment.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PaymentDbContext))]
[Migration("20261019000000_AllowMultiplePaymentsPerTrip")]
public sealed class AllowMultiplePaymentsPerTrip : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Payments_TripId",
            table: "Payments");

        migrationBuilder.CreateIndex(
            name: "IX_Payments_TripId",
            table: "Payments",
            column: "TripId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Payments_TripId",
            table: "Payments");

        migrationBuilder.CreateIndex(
            name: "IX_Payments_TripId",
            table: "Payments",
            column: "TripId",
            unique: true,
            filter: "\"TripId\" IS NOT NULL");
    }
}

