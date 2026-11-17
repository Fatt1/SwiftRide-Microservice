using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trip.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedTripFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "LastRejectedDriverId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "LastRejectedOfferId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "PaymentFailureReason",
                table: "Trips");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CorrelationId",
                table: "Trips",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "LastRejectedDriverId",
                table: "Trips",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastRejectedOfferId",
                table: "Trips",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentFailureReason",
                table: "Trips",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
