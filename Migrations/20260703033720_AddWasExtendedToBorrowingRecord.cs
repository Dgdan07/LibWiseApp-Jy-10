using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibWiseApp.Migrations
{
    /// <inheritdoc />
    public partial class AddWasExtendedToBorrowingRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WasExtended",
                table: "BorrowingRecords",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WasExtended",
                table: "BorrowingRecords");
        }
    }
}
