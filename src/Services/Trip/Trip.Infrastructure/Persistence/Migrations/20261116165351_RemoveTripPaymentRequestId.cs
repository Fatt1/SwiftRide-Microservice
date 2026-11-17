using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trip.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTripPaymentRequestId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinalFare",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "PaymentRequestId",
                table: "Trips");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FinalFare",
                table: "Trips",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentRequestId",
                table: "Trips",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
