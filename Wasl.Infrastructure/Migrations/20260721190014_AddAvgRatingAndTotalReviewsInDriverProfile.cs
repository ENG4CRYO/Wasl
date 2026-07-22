using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wasl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAvgRatingAndTotalReviewsInDriverProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvgRating",
                table: "Rides");

            migrationBuilder.AddColumn<decimal>(
                name: "AverageRating",
                table: "DriverProfiles",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TotalReviews",
                table: "DriverProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "DriverProfiles");

            migrationBuilder.DropColumn(
                name: "TotalReviews",
                table: "DriverProfiles");

            migrationBuilder.AddColumn<decimal>(
                name: "AvgRating",
                table: "Rides",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
