using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ghayal_Bhaag.Migrations
{
    /// <inheritdoc />
    public partial class Rename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "status",
                table: "CartItem",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "stock",
                table: "Book",
                newName: "Stock");

            migrationBuilder.RenameColumn(
                name: "publisher",
                table: "Book",
                newName: "Publisher");

            migrationBuilder.RenameColumn(
                name: "price",
                table: "Book",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "language",
                table: "Book",
                newName: "Language");

            migrationBuilder.RenameColumn(
                name: "genre",
                table: "Book",
                newName: "Genre");

            migrationBuilder.RenameColumn(
                name: "format",
                table: "Book",
                newName: "Format");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "Book",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "author",
                table: "Book",
                newName: "Author");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "Book",
                newName: "Image");

            migrationBuilder.RenameColumn(
                name: "physical_access",
                table: "Book",
                newName: "Sale");

            migrationBuilder.RenameColumn(
                name: "on_sale",
                table: "Book",
                newName: "PhysicalAccess");

            migrationBuilder.RenameColumn(
                name: "new_arrival",
                table: "Book",
                newName: "NewArrival");

            migrationBuilder.RenameColumn(
                name: "imageUrl",
                table: "Book",
                newName: "BookTitle");

            migrationBuilder.RenameColumn(
                name: "discount",
                table: "Book",
                newName: "DiscountAmount");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "Anouncement",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "Anouncement",
                newName: "Description");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "CartItem",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Stock",
                table: "Book",
                newName: "stock");

            migrationBuilder.RenameColumn(
                name: "Publisher",
                table: "Book",
                newName: "publisher");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Book",
                newName: "price");

            migrationBuilder.RenameColumn(
                name: "Language",
                table: "Book",
                newName: "language");

            migrationBuilder.RenameColumn(
                name: "Genre",
                table: "Book",
                newName: "genre");

            migrationBuilder.RenameColumn(
                name: "Format",
                table: "Book",
                newName: "format");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Book",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Author",
                table: "Book",
                newName: "author");

            migrationBuilder.RenameColumn(
                name: "Sale",
                table: "Book",
                newName: "physical_access");

            migrationBuilder.RenameColumn(
                name: "PhysicalAccess",
                table: "Book",
                newName: "on_sale");

            migrationBuilder.RenameColumn(
                name: "NewArrival",
                table: "Book",
                newName: "new_arrival");

            migrationBuilder.RenameColumn(
                name: "Image",
                table: "Book",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "DiscountAmount",
                table: "Book",
                newName: "discount");

            migrationBuilder.RenameColumn(
                name: "BookTitle",
                table: "Book",
                newName: "imageUrl");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Anouncement",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Anouncement",
                newName: "description");
        }
    }
}
