using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovelManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterEventAndCharacterHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "History",
                table: "Characters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeyEvents",
                table: "Characters",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CharacterEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CharacterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StoryTime = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    ChapterId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PlotId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Impact = table.Column<string>(type: "TEXT", nullable: true),
                    InvolvedCharacterIds = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterEvents_Chapters_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CharacterEvents_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterEvents_Plots_PlotId",
                        column: x => x.PlotId,
                        principalTable: "Plots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEvents_ChapterId",
                table: "CharacterEvents",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEvents_CharacterId",
                table: "CharacterEvents",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEvents_CharacterId_Order",
                table: "CharacterEvents",
                columns: new[] { "CharacterId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEvents_EventType",
                table: "CharacterEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEvents_Order",
                table: "CharacterEvents",
                column: "Order");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEvents_PlotId",
                table: "CharacterEvents",
                column: "PlotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterEvents");

            migrationBuilder.DropColumn(
                name: "History",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "KeyEvents",
                table: "Characters");
        }
    }
}
