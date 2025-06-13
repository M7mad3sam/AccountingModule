using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspNetCoreMvcTemplate.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJournalEntryTotals2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalDebit",
                table: "JournalEntries",
                newName: "DebitTotal");

            migrationBuilder.RenameColumn(
                name: "TotalCredit",
                table: "JournalEntries",
                newName: "CreditTotal");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DebitTotal",
                table: "JournalEntries",
                newName: "TotalDebit");

            migrationBuilder.RenameColumn(
                name: "CreditTotal",
                table: "JournalEntries",
                newName: "TotalCredit");
        }
    }
}
