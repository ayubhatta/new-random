using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookHavenLibrary.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePurchase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Purchases_BookId",
                table: "Purchases",
                column: "BookId");

            migrationBuilder.AddForeignKey(
                name: "FK_Purchases_Books_BookId",
                table: "Purchases",
                column: "BookId",
                principalTable: "Books",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Purchases_Books_BookId",
                table: "Purchases");

            migrationBuilder.DropIndex(
                name: "IX_Purchases_BookId",
                table: "Purchases");
        }
    }
}
