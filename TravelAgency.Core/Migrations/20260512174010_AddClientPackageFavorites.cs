using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TravelAgency.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddClientPackageFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientPackageFavorites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientUsername = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TripPackageId = table.Column<int>(type: "integer", nullable: false),
                    SavedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientPackageFavorites", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientPackageFavorites_ClientUsername_TripPackageId",
                table: "ClientPackageFavorites",
                columns: new[] { "ClientUsername", "TripPackageId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientPackageFavorites");
        }
    }
}
