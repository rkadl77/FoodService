using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitsApplication.Migrations
{
    /// <inheritdoc />
    public partial class CreateCartItemsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cart_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionId = table.Column<string>(type: "text", nullable: false),
                    DishId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cart_items", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cart_items_DishId",
                table: "cart_items",
                column: "DishId");

            migrationBuilder.CreateIndex(
                name: "IX_cart_items_SessionId",
                table: "cart_items",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_cart_items_UserId",
                table: "cart_items",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_cart_items_UserId_SessionId_DishId",
                table: "cart_items",
                columns: new[] { "UserId", "SessionId", "DishId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cart_items");
        }
    }
}
