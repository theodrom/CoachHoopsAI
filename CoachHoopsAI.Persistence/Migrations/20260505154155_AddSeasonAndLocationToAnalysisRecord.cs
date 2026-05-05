using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachHoopsAI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSeasonAndLocationToAnalysisRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Analyses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Season",
                table: "Analyses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "Analyses");

            migrationBuilder.DropColumn(
                name: "Season",
                table: "Analyses");
        }
    }
}
