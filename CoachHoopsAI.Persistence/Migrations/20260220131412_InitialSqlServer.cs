using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachHoopsAI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Analyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RequestedRulesProfile = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AppliedRulesProfile = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    GameDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TeamName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    OpponentName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    RulesetVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PromptVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AiModel = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProblemTagsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiagnosticsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SuggestionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Analyses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Analyses_CreatedUtc",
                table: "Analyses",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Analyses_GameDate",
                table: "Analyses",
                column: "GameDate");

            migrationBuilder.CreateIndex(
                name: "IX_Analyses_TeamName",
                table: "Analyses",
                column: "TeamName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Analyses");
        }
    }
}
