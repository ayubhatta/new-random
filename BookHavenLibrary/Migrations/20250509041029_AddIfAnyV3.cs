using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookHavenLibrary.Migrations
{
    /// <inheritdoc />
    public partial class AddIfAnyV3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Inventory_BookId",
                table: "Inventory",
                column: "BookId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventory_Books_BookId",
                table: "Inventory",
                column: "BookId",
                principalTable: "Books",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inventory_Books_BookId",
                table: "Inventory");

            migrationBuilder.DropIndex(
                name: "IX_Inventory_BookId",
                table: "Inventory");
        }
    }
}
