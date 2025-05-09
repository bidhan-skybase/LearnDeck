using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ghayal_Bhaag.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorandImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "author",
                table: "Book",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "imageUrl",
                table: "Book",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "author",
                table: "Book");

            migrationBuilder.DropColumn(
                name: "imageUrl",
                table: "Book");
        }
    }
}
