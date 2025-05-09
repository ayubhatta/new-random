using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookHavenLibrary.Migrations
{
    /// <inheritdoc />
    public partial class AddOnSaleinDiscount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OnSale",
                table: "Discounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnSale",
                table: "Discounts");
        }
    }
}
