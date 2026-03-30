using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgency.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddTripPackageUiFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "BasePrice",
                table: "TripPackages",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "TripPackages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PricingNotes",
                table: "TripPackages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShortDescription",
                table: "TripPackages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TripType",
                table: "TripPackages",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BasePrice",
                table: "TripPackages");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "TripPackages");

            migrationBuilder.DropColumn(
                name: "PricingNotes",
                table: "TripPackages");

            migrationBuilder.DropColumn(
                name: "ShortDescription",
                table: "TripPackages");

            migrationBuilder.DropColumn(
                name: "TripType",
                table: "TripPackages");
        }
    }
}
