using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgency.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddTripPackageCoverImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverImageUrl",
                table: "TripPackages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverImageUrl",
                table: "TripPackages");
        }
    }
}
