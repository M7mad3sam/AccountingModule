using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspNetCoreMvcTemplate.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModelRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FiscalPeriods_AspNetUsers_ModifiedById",
                table: "FiscalPeriods");

            migrationBuilder.DropForeignKey(
                name: "FK_FiscalYears_AspNetUsers_ModifiedById",
                table: "FiscalYears");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_AspNetUsers_ModifiedById",
                table: "JournalEntries");

            migrationBuilder.AlterColumn<string>(
                name: "ModifiedById",
                table: "JournalEntries",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "ModifiedById",
                table: "FiscalYears",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "ModifiedById",
                table: "FiscalPeriods",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalPeriods_AspNetUsers_ModifiedById",
                table: "FiscalPeriods",
                column: "ModifiedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalYears_AspNetUsers_ModifiedById",
                table: "FiscalYears",
                column: "ModifiedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_AspNetUsers_ModifiedById",
                table: "JournalEntries",
                column: "ModifiedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FiscalPeriods_AspNetUsers_ModifiedById",
                table: "FiscalPeriods");

            migrationBuilder.DropForeignKey(
                name: "FK_FiscalYears_AspNetUsers_ModifiedById",
                table: "FiscalYears");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_AspNetUsers_ModifiedById",
                table: "JournalEntries");

            migrationBuilder.AlterColumn<string>(
                name: "ModifiedById",
                table: "JournalEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ModifiedById",
                table: "FiscalYears",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ModifiedById",
                table: "FiscalPeriods",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalPeriods_AspNetUsers_ModifiedById",
                table: "FiscalPeriods",
                column: "ModifiedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalYears_AspNetUsers_ModifiedById",
                table: "FiscalYears",
                column: "ModifiedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_AspNetUsers_ModifiedById",
                table: "JournalEntries",
                column: "ModifiedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
