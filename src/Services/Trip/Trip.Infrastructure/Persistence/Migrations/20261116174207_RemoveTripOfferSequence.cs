using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trip.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTripOfferSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OfferSequence",
                table: "Trips");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OfferSequence",
                table: "Trips",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
