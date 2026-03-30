using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelAgency.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddTripPackageExtraFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccommodationName",
                table: "TripPackages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "AvailableSeats",
                table: "TripPackages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "TripPackages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DepartureCity",
                table: "TripPackages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Destination",
                table: "TripPackages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "DiscountPercent",
                table: "TripPackages",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ExtraCharges",
                table: "TripPackages",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "MealPlan",
                table: "TripPackages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "VatPercent",
                table: "TripPackages",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccommodationName",
                table: "TripPackages");

            migrationBuilder.DropColumn(
                name: "AvailableSeats",
                table: "TripPackages");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "TripPackages");

            migrationBuilder.DropColumn(
                name: "DepartureCity",
                table: "TripPackages");

            migrationBuilder.DropColumn(
                name: "Destination",
                table: "TripPackages");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "TripPackages");

            migrationBuilder.DropColumn(
                name: "ExtraCharges",
                table: "TripPackages");

            migrationBuilder.DropColumn(
                name: "MealPlan",
                table: "TripPackages");

            migrationBuilder.DropColumn(
                name: "VatPercent",
                table: "TripPackages");
        }
    }
}
