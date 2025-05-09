using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookHavenLibrary.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPaymentDoneInCart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPaymentDone",
                table: "ShoppingCarts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPaymentDone",
                table: "ShoppingCarts");
        }
    }
}
