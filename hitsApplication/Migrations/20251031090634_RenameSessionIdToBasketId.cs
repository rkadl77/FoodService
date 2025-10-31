using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace hitsApplication.Migrations
{
    /// <inheritdoc />
    public partial class RenameSessionIdToBasketId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_cart_items_UserId_SessionId_DishId",
                table: "cart_items");

            migrationBuilder.RenameColumn(
                name: "SessionId",
                table: "cart_items",
                newName: "BasketId");

            migrationBuilder.RenameIndex(
                name: "IX_cart_items_SessionId",
                table: "cart_items",
                newName: "IX_cart_items_BasketId");

            migrationBuilder.CreateIndex(
                name: "IX_cart_items_BasketId_DishId",
                table: "cart_items",
                columns: new[] { "BasketId", "DishId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_cart_items_BasketId_DishId",
                table: "cart_items");

            migrationBuilder.RenameColumn(
                name: "BasketId",
                table: "cart_items",
                newName: "SessionId");

            migrationBuilder.RenameIndex(
                name: "IX_cart_items_BasketId",
                table: "cart_items",
                newName: "IX_cart_items_SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_cart_items_UserId_SessionId_DishId",
                table: "cart_items",
                columns: new[] { "UserId", "SessionId", "DishId" },
                unique: true);
        }
    }
}
