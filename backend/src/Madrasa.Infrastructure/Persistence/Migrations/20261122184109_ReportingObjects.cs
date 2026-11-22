using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Madrasa.Infrastructure.Persistence.Migrations
{
    /// <summary>Views, a function and stored procedures for reports and maintenance (Persistence/Sql/ReportingObjects.sql).</summary>
    public partial class ReportingObjects : Migration
    {
        private static readonly string[] Objects =
        [
            "PROCEDURE dbo.usp_BackupDatabase", "PROCEDURE dbo.usp_DashboardSummary", "PROCEDURE dbo.usp_SalarySheet",
            "PROCEDURE dbo.usp_ExamMeritList", "PROCEDURE dbo.usp_LedgerStatement", "PROCEDURE dbo.usp_DueList",
            "PROCEDURE dbo.usp_MonthlyCollection", "PROCEDURE dbo.usp_StudentAttendance", "PROCEDURE dbo.usp_DailyAttendance",
            "VIEW dbo.vw_MonthlyLedger", "VIEW dbo.vw_FundBalances", "VIEW dbo.vw_FeeDues", "VIEW dbo.vw_CurrentStudents",
            "FUNCTION dbo.fn_StudentDue", "FUNCTION dbo.fn_CurrentSessionId",
        ];

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Each GO-separated batch runs on its own through EXEC, because CREATE VIEW/PROCEDURE must start a batch
            // and the idempotent script (database/MMS.sql) puts every migration statement inside an IF block
            foreach (var batch in Batches()) migrationBuilder.Sql($"EXEC(N'{batch.Replace("'", "''")}');");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var o in Objects) migrationBuilder.Sql($"DROP {o.Split(' ')[0]} IF EXISTS {o.Split(' ')[1]};");
        }

        internal static IEnumerable<string> Batches()
        {
            using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("Madrasa.Infrastructure.Persistence.Sql.ReportingObjects.sql")
                ?? throw new InvalidOperationException("ReportingObjects.sql is not embedded");
            var sql = new StreamReader(s).ReadToEnd().Replace("\r\n", "\n");
            return System.Text.RegularExpressions.Regex.Split(sql, @"^\s*GO\s*$", System.Text.RegularExpressions.RegexOptions.Multiline)
                .Select(b => b.Trim()).Where(b => b.Length > 0 && !b.Split('\n').All(l => l.TrimStart().StartsWith("--") || l.Trim().Length == 0));
        }
    }
}
