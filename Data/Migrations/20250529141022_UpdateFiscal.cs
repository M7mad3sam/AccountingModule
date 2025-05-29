using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspNetCoreMvcTemplate.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFiscal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FiscalPeriods_AspNetUsers_ClosedById",
                table: "FiscalPeriods");

            migrationBuilder.DropForeignKey(
                name: "FK_FiscalYears_AspNetUsers_ClosedById",
                table: "FiscalYears");

            migrationBuilder.AlterColumn<string>(
                name: "ClosedById",
                table: "FiscalYears",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "ClosedById",
                table: "FiscalPeriods",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalPeriods_AspNetUsers_ClosedById",
                table: "FiscalPeriods",
                column: "ClosedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalYears_AspNetUsers_ClosedById",
                table: "FiscalYears",
                column: "ClosedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FiscalPeriods_AspNetUsers_ClosedById",
                table: "FiscalPeriods");

            migrationBuilder.DropForeignKey(
                name: "FK_FiscalYears_AspNetUsers_ClosedById",
                table: "FiscalYears");

            migrationBuilder.AlterColumn<string>(
                name: "ClosedById",
                table: "FiscalYears",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClosedById",
                table: "FiscalPeriods",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalPeriods_AspNetUsers_ClosedById",
                table: "FiscalPeriods",
                column: "ClosedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalYears_AspNetUsers_ClosedById",
                table: "FiscalYears",
                column: "ClosedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
