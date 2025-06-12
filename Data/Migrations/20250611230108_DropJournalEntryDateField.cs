using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspNetCoreMvcTemplate.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropJournalEntryDateField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Date",
                table: "JournalEntries",
                newName: "TransactionDate");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "JournalEntries",
                newName: "CreatedUtc");

            migrationBuilder.RenameColumn(
                name: "ApprovedAt",
                table: "JournalEntries",
                newName: "ApprovedUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TransactionDate",
                table: "JournalEntries",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "CreatedUtc",
                table: "JournalEntries",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ApprovedUtc",
                table: "JournalEntries",
                newName: "ApprovedAt");
        }
    }
}
