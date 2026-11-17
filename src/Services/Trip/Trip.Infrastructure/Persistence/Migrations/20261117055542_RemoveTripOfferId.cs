using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trip.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTripOfferId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentOfferId",
                table: "Trips");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentOfferId",
                table: "Trips",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
