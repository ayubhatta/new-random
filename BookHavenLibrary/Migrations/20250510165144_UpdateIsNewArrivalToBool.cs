using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookHavenLibrary.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIsNewArrivalToBool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewArrival",
                table: "Books");

            migrationBuilder.AddColumn<bool>(
                name: "NewArrival",
                table: "Books",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewArrival",
                table: "Books");

            migrationBuilder.AddColumn<DateTime>(
                name: "NewArrival",
                table: "Books",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: DateTime.UtcNow); // Or whatever default makes sense
        }

    }
}
