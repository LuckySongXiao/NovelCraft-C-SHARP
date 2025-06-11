using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovelManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAgentArchitecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Characters_FirstAppearanceChapterId",
                table: "Characters",
                column: "FirstAppearanceChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_LastAppearanceChapterId",
                table: "Characters",
                column: "LastAppearanceChapterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Characters_FirstAppearanceChapterId",
                table: "Characters");

            migrationBuilder.DropIndex(
                name: "IX_Characters_LastAppearanceChapterId",
                table: "Characters");
        }
    }
}
