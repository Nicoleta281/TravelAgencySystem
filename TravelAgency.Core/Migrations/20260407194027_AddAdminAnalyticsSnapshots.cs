using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TravelAgency.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminAnalyticsSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminAnalyticsSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SavedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalBookings = table.Column<int>(type: "integer", nullable: false),
                    ConfirmedBookings = table.Column<int>(type: "integer", nullable: false),
                    RejectedBookings = table.Column<int>(type: "integer", nullable: false),
                    TotalRevenue = table.Column<double>(type: "double precision", nullable: false),
                    TotalUsers = table.Column<int>(type: "integer", nullable: false),
                    ActiveUsers = table.Column<int>(type: "integer", nullable: false),
                    BlockedUsers = table.Column<int>(type: "integer", nullable: false),
                    TopDestination = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAnalyticsSnapshots", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminAnalyticsSnapshots");
        }
    }
}
