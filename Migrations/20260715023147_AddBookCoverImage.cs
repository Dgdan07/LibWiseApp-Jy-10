using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibWiseApp.Migrations
{
    /// <inheritdoc />
    public partial class AddBookCoverImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "CoverImage",
                table: "Books",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverImageContentType",
                table: "Books",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverImage",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "CoverImageContentType",
                table: "Books");
        }
    }
}
