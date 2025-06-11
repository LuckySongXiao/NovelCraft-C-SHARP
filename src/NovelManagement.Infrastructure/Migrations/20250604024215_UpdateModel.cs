using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovelManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Volumes_OrderIndex",
                table: "Volumes");

            migrationBuilder.DropIndex(
                name: "IX_PoliticalSystems_OrderIndex",
                table: "PoliticalSystems");

            migrationBuilder.RenameColumn(
                name: "OrderIndex",
                table: "WorldSettings",
                newName: "Order");

            migrationBuilder.RenameIndex(
                name: "IX_WorldSettings_OrderIndex",
                table: "WorldSettings",
                newName: "IX_WorldSettings_Order");

            migrationBuilder.RenameColumn(
                name: "OrderIndex",
                table: "Volumes",
                newName: "WordCount");

            migrationBuilder.RenameColumn(
                name: "OrderIndex",
                table: "PoliticalSystems",
                newName: "Stability");

            migrationBuilder.RenameColumn(
                name: "OrderIndex",
                table: "CultivationSystems",
                newName: "Order");

            migrationBuilder.RenameIndex(
                name: "IX_CultivationSystems_OrderIndex",
                table: "CultivationSystems",
                newName: "IX_CultivationSystems_Order");

            migrationBuilder.RenameColumn(
                name: "OrderIndex",
                table: "CultivationLevels",
                newName: "Order");

            migrationBuilder.RenameIndex(
                name: "IX_CultivationLevels_OrderIndex",
                table: "CultivationLevels",
                newName: "IX_CultivationLevels_Order");

            migrationBuilder.RenameColumn(
                name: "OrderIndex",
                table: "Chapters",
                newName: "Order");

            migrationBuilder.RenameIndex(
                name: "IX_Chapters_OrderIndex",
                table: "Chapters",
                newName: "IX_Chapters_Order");

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Volumes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Progress",
                table: "Projects",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Hierarchy",
                table: "PoliticalSystems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Influence",
                table: "PoliticalSystems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "PoliticalSystems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Importance",
                table: "Factions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Influence",
                table: "Factions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentFactionId",
                table: "Factions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PowerLevel",
                table: "Factions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "BaseValue",
                table: "CurrencySystems",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Stability",
                table: "CurrencySystems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "CurrencySystems",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "CurrencySystems",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Difficulty",
                table: "CultivationSystems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxLevel",
                table: "CultivationSystems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Volumes_Order",
                table: "Volumes",
                column: "Order");

            migrationBuilder.CreateIndex(
                name: "IX_PoliticalSystems_Order",
                table: "PoliticalSystems",
                column: "Order");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Volumes_Order",
                table: "Volumes");

            migrationBuilder.DropIndex(
                name: "IX_PoliticalSystems_Order",
                table: "PoliticalSystems");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "Volumes");

            migrationBuilder.DropColumn(
                name: "Progress",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Hierarchy",
                table: "PoliticalSystems");

            migrationBuilder.DropColumn(
                name: "Influence",
                table: "PoliticalSystems");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "PoliticalSystems");

            migrationBuilder.DropColumn(
                name: "Importance",
                table: "Factions");

            migrationBuilder.DropColumn(
                name: "Influence",
                table: "Factions");

            migrationBuilder.DropColumn(
                name: "ParentFactionId",
                table: "Factions");

            migrationBuilder.DropColumn(
                name: "PowerLevel",
                table: "Factions");

            migrationBuilder.DropColumn(
                name: "BaseValue",
                table: "CurrencySystems");

            migrationBuilder.DropColumn(
                name: "Stability",
                table: "CurrencySystems");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CurrencySystems");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "CurrencySystems");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                table: "CultivationSystems");

            migrationBuilder.DropColumn(
                name: "MaxLevel",
                table: "CultivationSystems");

            migrationBuilder.RenameColumn(
                name: "Order",
                table: "WorldSettings",
                newName: "OrderIndex");

            migrationBuilder.RenameIndex(
                name: "IX_WorldSettings_Order",
                table: "WorldSettings",
                newName: "IX_WorldSettings_OrderIndex");

            migrationBuilder.RenameColumn(
                name: "WordCount",
                table: "Volumes",
                newName: "OrderIndex");

            migrationBuilder.RenameColumn(
                name: "Stability",
                table: "PoliticalSystems",
                newName: "OrderIndex");

            migrationBuilder.RenameColumn(
                name: "Order",
                table: "CultivationSystems",
                newName: "OrderIndex");

            migrationBuilder.RenameIndex(
                name: "IX_CultivationSystems_Order",
                table: "CultivationSystems",
                newName: "IX_CultivationSystems_OrderIndex");

            migrationBuilder.RenameColumn(
                name: "Order",
                table: "CultivationLevels",
                newName: "OrderIndex");

            migrationBuilder.RenameIndex(
                name: "IX_CultivationLevels_Order",
                table: "CultivationLevels",
                newName: "IX_CultivationLevels_OrderIndex");

            migrationBuilder.RenameColumn(
                name: "Order",
                table: "Chapters",
                newName: "OrderIndex");

            migrationBuilder.RenameIndex(
                name: "IX_Chapters_Order",
                table: "Chapters",
                newName: "IX_Chapters_OrderIndex");

            migrationBuilder.CreateIndex(
                name: "IX_Volumes_OrderIndex",
                table: "Volumes",
                column: "OrderIndex");

            migrationBuilder.CreateIndex(
                name: "IX_PoliticalSystems_OrderIndex",
                table: "PoliticalSystems",
                column: "OrderIndex");
        }
    }
}
