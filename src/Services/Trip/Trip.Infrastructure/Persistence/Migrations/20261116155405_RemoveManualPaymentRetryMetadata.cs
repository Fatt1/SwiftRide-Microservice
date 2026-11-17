using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trip.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveManualPaymentRetryMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentRequestFingerprint",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "RetrySamePaymentRequest",
                table: "Trips");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentRequestFingerprint",
                table: "Trips",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RetrySamePaymentRequest",
                table: "Trips",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
