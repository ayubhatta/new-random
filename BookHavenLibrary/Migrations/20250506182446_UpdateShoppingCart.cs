using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookHavenLibrary.Migrations
{
    /// <inheritdoc />
    public partial class UpdateShoppingCart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CartId",
                table: "ShoppingCarts");

            migrationBuilder.RenameColumn(
                name: "CartId",
                table: "CartItems",
                newName: "ShoppingCartId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShoppingCartId",
                table: "CartItems",
                newName: "CartId");

            migrationBuilder.AddColumn<int>(
                name: "CartId",
                table: "ShoppingCarts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
