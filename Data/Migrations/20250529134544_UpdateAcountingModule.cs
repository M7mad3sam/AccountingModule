using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspNetCoreMvcTemplate.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAcountingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "Vendors",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "Vendors",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CommercialRegistration",
                table: "Vendors",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactPerson",
                table: "Vendors",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IBAN",
                table: "Vendors",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Vendors",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaymentTerms",
                table: "Vendors",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "SubjectToWithholdingTax",
                table: "Vendors",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Vendors",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VendorType",
                table: "Vendors",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "TaxRates",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveFrom",
                table: "TaxRates",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveTo",
                table: "TaxRates",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Credit",
                table: "JournalEntryLines",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Debit",
                table: "JournalEntryLines",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WithholdingTaxAmount",
                table: "JournalEntryLines",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WithholdingTaxId",
                table: "JournalEntryLines",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalNotes",
                table: "JournalEntries",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedDate",
                table: "JournalEntries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentUrl",
                table: "JournalEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ClientId",
                table: "JournalEntries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "JournalEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "JournalEntries",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndRecurrenceDate",
                table: "JournalEntries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EntryDate",
                table: "JournalEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "JournalEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsRecurring",
                table: "JournalEntries",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemGenerated",
                table: "JournalEntries",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedById",
                table: "JournalEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "JournalEntries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRecurrenceDate",
                table: "JournalEntries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "JournalEntries",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PostedById",
                table: "JournalEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PostedDate",
                table: "JournalEntries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PostingDate",
                table: "JournalEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "RecurrencePattern",
                table: "JournalEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SourceDocument",
                table: "JournalEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "VendorId",
                table: "JournalEntries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosedById",
                table: "FiscalYears",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedDate",
                table: "FiscalYears",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "FiscalYears",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "FiscalYears",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "FiscalYears",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "FiscalYears",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedById",
                table: "FiscalYears",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "FiscalYears",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosedById",
                table: "FiscalPeriods",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedDate",
                table: "FiscalPeriods",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "FiscalPeriods",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "FiscalPeriods",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "FiscalPeriods",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "FiscalPeriods",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedById",
                table: "FiscalPeriods",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "FiscalPeriods",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClientType",
                table: "Clients",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CommercialRegistration",
                table: "Clients",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactPerson",
                table: "Clients",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CreditLimit",
                table: "Clients",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CreditPeriod",
                table: "Clients",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Clients",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsRetainedEarnings",
                table: "Accounts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "WithholdingTax",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NameEn = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Rate = table.Column<decimal>(type: "TEXT", nullable: false),
                    ApplicableVendorTypes = table.Column<string>(type: "TEXT", nullable: false),
                    MinimumThreshold = table.Column<decimal>(type: "TEXT", nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WithholdingTax", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_WithholdingTaxId",
                table: "JournalEntryLines",
                column: "WithholdingTaxId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_ClientId",
                table: "JournalEntries",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_ModifiedById",
                table: "JournalEntries",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_PostedById",
                table: "JournalEntries",
                column: "PostedById");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_VendorId",
                table: "JournalEntries",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYears_ClosedById",
                table: "FiscalYears",
                column: "ClosedById");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYears_CreatedById",
                table: "FiscalYears",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalYears_ModifiedById",
                table: "FiscalYears",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalPeriods_ClosedById",
                table: "FiscalPeriods",
                column: "ClosedById");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalPeriods_CreatedById",
                table: "FiscalPeriods",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalPeriods_ModifiedById",
                table: "FiscalPeriods",
                column: "ModifiedById");

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalPeriods_AspNetUsers_ClosedById",
                table: "FiscalPeriods",
                column: "ClosedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalPeriods_AspNetUsers_CreatedById",
                table: "FiscalPeriods",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalPeriods_AspNetUsers_ModifiedById",
                table: "FiscalPeriods",
                column: "ModifiedById",
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

            migrationBuilder.AddForeignKey(
                name: "FK_FiscalYears_AspNetUsers_CreatedById",
                table: "FiscalYears",
                column: "CreatedById",
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

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_AspNetUsers_PostedById",
                table: "JournalEntries",
                column: "PostedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_Clients_ClientId",
                table: "JournalEntries",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntries_Vendors_VendorId",
                table: "JournalEntries",
                column: "VendorId",
                principalTable: "Vendors",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntryLines_WithholdingTax_WithholdingTaxId",
                table: "JournalEntryLines",
                column: "WithholdingTaxId",
                principalTable: "WithholdingTax",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FiscalPeriods_AspNetUsers_ClosedById",
                table: "FiscalPeriods");

            migrationBuilder.DropForeignKey(
                name: "FK_FiscalPeriods_AspNetUsers_CreatedById",
                table: "FiscalPeriods");

            migrationBuilder.DropForeignKey(
                name: "FK_FiscalPeriods_AspNetUsers_ModifiedById",
                table: "FiscalPeriods");

            migrationBuilder.DropForeignKey(
                name: "FK_FiscalYears_AspNetUsers_ClosedById",
                table: "FiscalYears");

            migrationBuilder.DropForeignKey(
                name: "FK_FiscalYears_AspNetUsers_CreatedById",
                table: "FiscalYears");

            migrationBuilder.DropForeignKey(
                name: "FK_FiscalYears_AspNetUsers_ModifiedById",
                table: "FiscalYears");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_AspNetUsers_ModifiedById",
                table: "JournalEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_AspNetUsers_PostedById",
                table: "JournalEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_Clients_ClientId",
                table: "JournalEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntries_Vendors_VendorId",
                table: "JournalEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntryLines_WithholdingTax_WithholdingTaxId",
                table: "JournalEntryLines");

            migrationBuilder.DropTable(
                name: "WithholdingTax");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntryLines_WithholdingTaxId",
                table: "JournalEntryLines");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_ClientId",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_ModifiedById",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_PostedById",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntries_VendorId",
                table: "JournalEntries");

            migrationBuilder.DropIndex(
                name: "IX_FiscalYears_ClosedById",
                table: "FiscalYears");

            migrationBuilder.DropIndex(
                name: "IX_FiscalYears_CreatedById",
                table: "FiscalYears");

            migrationBuilder.DropIndex(
                name: "IX_FiscalYears_ModifiedById",
                table: "FiscalYears");

            migrationBuilder.DropIndex(
                name: "IX_FiscalPeriods_ClosedById",
                table: "FiscalPeriods");

            migrationBuilder.DropIndex(
                name: "IX_FiscalPeriods_CreatedById",
                table: "FiscalPeriods");

            migrationBuilder.DropIndex(
                name: "IX_FiscalPeriods_ModifiedById",
                table: "FiscalPeriods");

            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "CommercialRegistration",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "ContactPerson",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "IBAN",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "PaymentTerms",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "SubjectToWithholdingTax",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "VendorType",
                table: "Vendors");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "TaxRates");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "TaxRates");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "TaxRates");

            migrationBuilder.DropColumn(
                name: "Credit",
                table: "JournalEntryLines");

            migrationBuilder.DropColumn(
                name: "Debit",
                table: "JournalEntryLines");

            migrationBuilder.DropColumn(
                name: "WithholdingTaxAmount",
                table: "JournalEntryLines");

            migrationBuilder.DropColumn(
                name: "WithholdingTaxId",
                table: "JournalEntryLines");

            migrationBuilder.DropColumn(
                name: "ApprovalNotes",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "ApprovedDate",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "AttachmentUrl",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "EndRecurrenceDate",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "EntryDate",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "IsRecurring",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "IsSystemGenerated",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "ModifiedById",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "NextRecurrenceDate",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "PostedById",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "PostedDate",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "PostingDate",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "RecurrencePattern",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "SourceDocument",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "VendorId",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "ClosedById",
                table: "FiscalYears");

            migrationBuilder.DropColumn(
                name: "ClosedDate",
                table: "FiscalYears");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "FiscalYears");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "FiscalYears");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "FiscalYears");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "FiscalYears");

            migrationBuilder.DropColumn(
                name: "ModifiedById",
                table: "FiscalYears");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "FiscalYears");

            migrationBuilder.DropColumn(
                name: "ClosedById",
                table: "FiscalPeriods");

            migrationBuilder.DropColumn(
                name: "ClosedDate",
                table: "FiscalPeriods");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "FiscalPeriods");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "FiscalPeriods");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "FiscalPeriods");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "FiscalPeriods");

            migrationBuilder.DropColumn(
                name: "ModifiedById",
                table: "FiscalPeriods");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "FiscalPeriods");

            migrationBuilder.DropColumn(
                name: "ClientType",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "CommercialRegistration",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "ContactPerson",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "CreditLimit",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "CreditPeriod",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "IsRetainedEarnings",
                table: "Accounts");
        }
    }
}
