using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trip.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEnRouteState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Trips SET Status = 'DriverAccepted' WHERE Status = 'EnRoute'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Original EnRoute rows cannot be distinguished from existing DriverAccepted rows.
        }
    }
}
