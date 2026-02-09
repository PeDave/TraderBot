using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TraderBot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Candles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Open = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    High = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Low = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Close = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Volume = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    TimeFrame = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExternalOrderId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Side = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: true),
                    FilledPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FilledAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Symbol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    EntryPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 8, nullable: false),
                    Side = table.Column<int>(type: "INTEGER", nullable: false),
                    MartingaleStep = table.Column<int>(type: "INTEGER", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsOpen = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Candles_Symbol_Timestamp",
                table: "Candles",
                columns: new[] { "Symbol", "Timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ExternalOrderId",
                table: "Orders",
                column: "ExternalOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_Symbol_IsOpen",
                table: "Positions",
                columns: new[] { "Symbol", "IsOpen" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Candles");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Positions");
        }
    }
}
