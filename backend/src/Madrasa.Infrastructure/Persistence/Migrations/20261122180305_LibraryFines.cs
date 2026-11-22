using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Madrasa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LibraryFines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BookIssueId",
                table: "LedgerEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinePaid",
                table: "BookIssues",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FineWaived",
                table: "BookIssues",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.InsertData(
                table: "LedgerCategories",
                columns: new[] { "Id", "Kind", "NameBn" },
                values: new object[] { 8, (byte)1, "লাইব্রেরি জরিমানা" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_BookIssueId",
                table: "LedgerEntries",
                column: "BookIssueId");

            migrationBuilder.AddForeignKey(
                name: "FK_LedgerEntries_BookIssues_BookIssueId",
                table: "LedgerEntries",
                column: "BookIssueId",
                principalTable: "BookIssues",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LedgerEntries_BookIssues_BookIssueId",
                table: "LedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_LedgerEntries_BookIssueId",
                table: "LedgerEntries");

            migrationBuilder.DeleteData(
                table: "LedgerCategories",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DropColumn(
                name: "BookIssueId",
                table: "LedgerEntries");

            migrationBuilder.DropColumn(
                name: "FinePaid",
                table: "BookIssues");

            migrationBuilder.DropColumn(
                name: "FineWaived",
                table: "BookIssues");
        }
    }
}
