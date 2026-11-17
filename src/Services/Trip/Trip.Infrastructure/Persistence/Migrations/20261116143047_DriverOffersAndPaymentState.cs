using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trip.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DriverOffersAndPaymentState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "QuoteId",
                table: "Trips",
                newName: "MatchSessionId");

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentOfferId",
                table: "Trips",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedMinutes",
                table: "Trips",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "Trips",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

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

            migrationBuilder.AddColumn<int>(
                name: "MaxAllowedRejections",
                table: "Trips",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OfferSequence",
                table: "Trips",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "OfferedDriverId",
                table: "Trips",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(name: "PaymentRequestFingerprint", table: "Trips", type: "nvarchar(64)", maxLength: 64, nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "PaymentRequestId",
                table: "Trips",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectCount",
                table: "Trips",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RetrySamePaymentRequest",
                table: "Trips",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "Version",
                table: "Trips",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
            migrationBuilder.Sql("UPDATE Trips SET Status = 'Failed', FailureReason = 'Legacy driver rejection; create a new trip to use offer matching.' WHERE Status = 'DriverRejected'");
            migrationBuilder.Sql("UPDATE Trips SET Status = 'PaymentPending' WHERE Status IN ('PaymentFailed', 'DroppedOff')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Trips SET Status = 'PaymentFailed' WHERE Status = 'PaymentPending'");
            migrationBuilder.Sql("UPDATE Trips SET Status = 'DriverRejected' WHERE Status = 'Failed'");
            migrationBuilder.Sql("UPDATE Trips SET Status = 'DriverAccepted' WHERE Status = 'EnRoute'");

            migrationBuilder.DropColumn(
                name: "CurrentOfferId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "EstimatedMinutes",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "LastRejectedDriverId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "LastRejectedOfferId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "MaxAllowedRejections",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "OfferSequence",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "OfferedDriverId",
                table: "Trips");

            migrationBuilder.DropColumn(name: "PaymentRequestFingerprint", table: "Trips");
            migrationBuilder.DropColumn(
                name: "PaymentRequestId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "RejectCount",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "RetrySamePaymentRequest",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Trips");

            migrationBuilder.RenameColumn(
                name: "MatchSessionId",
                table: "Trips",
                newName: "QuoteId");
        }
    }
}
