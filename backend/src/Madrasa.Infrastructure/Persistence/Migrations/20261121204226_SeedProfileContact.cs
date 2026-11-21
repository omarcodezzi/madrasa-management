using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Madrasa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedProfileContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MadrasaProfile",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Address", "Phone" },
                values: new object[] { "রামারবাগ কেন্দ্রিয় ঈদগা, রামারবাগ, উত্তরদা, লাকসাম, কুমিল্লা", "01975448094" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MadrasaProfile",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Address", "Phone" },
                values: new object[] { "রামারবাগ, লাকসাম, কুমিল্লা", null });
        }
    }
}
