using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Madrasa.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FeeReceipts_StudentId",
                table: "FeeReceipts");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDocuments_StudentId",
                table: "StudentDocuments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDiscounts_FeeHeadId",
                table: "StudentDiscounts",
                column: "FeeHeadId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDiscounts_SponsorFundId",
                table: "StudentDiscounts",
                column: "SponsorFundId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDiscounts_StudentId",
                table: "StudentDiscounts",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryPayments_FundId",
                table: "SalaryPayments",
                column: "FundId");

            migrationBuilder.CreateIndex(
                name: "IX_RoutineSlots_SessionId",
                table: "RoutineSlots",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageLogs_RelatedNoticeId",
                table: "MessageLogs",
                column: "RelatedNoticeId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_DonorId",
                table: "LedgerEntries",
                column: "DonorId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_FeeReceiptId",
                table: "LedgerEntries",
                column: "FeeReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_SalaryPaymentId",
                table: "LedgerEntries",
                column: "SalaryPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_HostelLeaves_StudentId",
                table: "HostelLeaves",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_HostelAllocations_StudentId",
                table: "HostelAllocations",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_HifzDailyLogs_TeacherId",
                table: "HifzDailyLogs",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeReceipts_StudentId_Date",
                table: "FeeReceipts",
                columns: new[] { "StudentId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_FeeReceiptLines_InvoiceId",
                table: "FeeReceiptLines",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeInvoices_Month",
                table: "FeeInvoices",
                column: "Month");

            migrationBuilder.CreateIndex(
                name: "IX_FeeInvoices_SessionId",
                table: "FeeInvoices",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Exams_SessionId",
                table: "Exams",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamResults_StudentId",
                table: "ExamResults",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamMarks_StudentId",
                table: "ExamMarks",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_BookIssues_StaffId",
                table: "BookIssues",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_BookIssues_StudentId",
                table: "BookIssues",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_SessionId",
                table: "AdmissionApplications",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_StudentId",
                table: "AdmissionApplications",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdmissionApplications_AcademicSessions_SessionId",
                table: "AdmissionApplications",
                column: "SessionId",
                principalTable: "AcademicSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AdmissionApplications_Students_StudentId",
                table: "AdmissionApplications",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BookIssues_Staff_StaffId",
                table: "BookIssues",
                column: "StaffId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BookIssues_Students_StudentId",
                table: "BookIssues",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamMarks_Exams_ExamId",
                table: "ExamMarks",
                column: "ExamId",
                principalTable: "Exams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamMarks_Students_StudentId",
                table: "ExamMarks",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamResults_Exams_ExamId",
                table: "ExamResults",
                column: "ExamId",
                principalTable: "Exams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamResults_Students_StudentId",
                table: "ExamResults",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Exams_AcademicSessions_SessionId",
                table: "Exams",
                column: "SessionId",
                principalTable: "AcademicSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FeeInvoices_AcademicSessions_SessionId",
                table: "FeeInvoices",
                column: "SessionId",
                principalTable: "AcademicSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FeeInvoices_Students_StudentId",
                table: "FeeInvoices",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FeeReceiptLines_FeeInvoices_InvoiceId",
                table: "FeeReceiptLines",
                column: "InvoiceId",
                principalTable: "FeeInvoices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HifzDailyLogs_Staff_TeacherId",
                table: "HifzDailyLogs",
                column: "TeacherId",
                principalTable: "Staff",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HifzDailyLogs_Students_StudentId",
                table: "HifzDailyLogs",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HifzJuzProgress_Students_StudentId",
                table: "HifzJuzProgress",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HostelAllocations_Students_StudentId",
                table: "HostelAllocations",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HostelLeaves_Students_StudentId",
                table: "HostelLeaves",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LedgerEntries_Donors_DonorId",
                table: "LedgerEntries",
                column: "DonorId",
                principalTable: "Donors",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LedgerEntries_FeeReceipts_FeeReceiptId",
                table: "LedgerEntries",
                column: "FeeReceiptId",
                principalTable: "FeeReceipts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LedgerEntries_SalaryPayments_SalaryPaymentId",
                table: "LedgerEntries",
                column: "SalaryPaymentId",
                principalTable: "SalaryPayments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MessageLogs_Notices_RelatedNoticeId",
                table: "MessageLogs",
                column: "RelatedNoticeId",
                principalTable: "Notices",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RoutineSlots_AcademicSessions_SessionId",
                table: "RoutineSlots",
                column: "SessionId",
                principalTable: "AcademicSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalaryPayments_Funds_FundId",
                table: "SalaryPayments",
                column: "FundId",
                principalTable: "Funds",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentDiscounts_FeeHeads_FeeHeadId",
                table: "StudentDiscounts",
                column: "FeeHeadId",
                principalTable: "FeeHeads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentDiscounts_Funds_SponsorFundId",
                table: "StudentDiscounts",
                column: "SponsorFundId",
                principalTable: "Funds",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentDiscounts_Students_StudentId",
                table: "StudentDiscounts",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentDocuments_Students_StudentId",
                table: "StudentDocuments",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdmissionApplications_AcademicSessions_SessionId",
                table: "AdmissionApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_AdmissionApplications_Students_StudentId",
                table: "AdmissionApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_BookIssues_Staff_StaffId",
                table: "BookIssues");

            migrationBuilder.DropForeignKey(
                name: "FK_BookIssues_Students_StudentId",
                table: "BookIssues");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamMarks_Exams_ExamId",
                table: "ExamMarks");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamMarks_Students_StudentId",
                table: "ExamMarks");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamResults_Exams_ExamId",
                table: "ExamResults");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamResults_Students_StudentId",
                table: "ExamResults");

            migrationBuilder.DropForeignKey(
                name: "FK_Exams_AcademicSessions_SessionId",
                table: "Exams");

            migrationBuilder.DropForeignKey(
                name: "FK_FeeInvoices_AcademicSessions_SessionId",
                table: "FeeInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_FeeInvoices_Students_StudentId",
                table: "FeeInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_FeeReceiptLines_FeeInvoices_InvoiceId",
                table: "FeeReceiptLines");

            migrationBuilder.DropForeignKey(
                name: "FK_HifzDailyLogs_Staff_TeacherId",
                table: "HifzDailyLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_HifzDailyLogs_Students_StudentId",
                table: "HifzDailyLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_HifzJuzProgress_Students_StudentId",
                table: "HifzJuzProgress");

            migrationBuilder.DropForeignKey(
                name: "FK_HostelAllocations_Students_StudentId",
                table: "HostelAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_HostelLeaves_Students_StudentId",
                table: "HostelLeaves");

            migrationBuilder.DropForeignKey(
                name: "FK_LedgerEntries_Donors_DonorId",
                table: "LedgerEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_LedgerEntries_FeeReceipts_FeeReceiptId",
                table: "LedgerEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_LedgerEntries_SalaryPayments_SalaryPaymentId",
                table: "LedgerEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_MessageLogs_Notices_RelatedNoticeId",
                table: "MessageLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_RoutineSlots_AcademicSessions_SessionId",
                table: "RoutineSlots");

            migrationBuilder.DropForeignKey(
                name: "FK_SalaryPayments_Funds_FundId",
                table: "SalaryPayments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentDiscounts_FeeHeads_FeeHeadId",
                table: "StudentDiscounts");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentDiscounts_Funds_SponsorFundId",
                table: "StudentDiscounts");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentDiscounts_Students_StudentId",
                table: "StudentDiscounts");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentDocuments_Students_StudentId",
                table: "StudentDocuments");

            migrationBuilder.DropIndex(
                name: "IX_StudentDocuments_StudentId",
                table: "StudentDocuments");

            migrationBuilder.DropIndex(
                name: "IX_StudentDiscounts_FeeHeadId",
                table: "StudentDiscounts");

            migrationBuilder.DropIndex(
                name: "IX_StudentDiscounts_SponsorFundId",
                table: "StudentDiscounts");

            migrationBuilder.DropIndex(
                name: "IX_StudentDiscounts_StudentId",
                table: "StudentDiscounts");

            migrationBuilder.DropIndex(
                name: "IX_SalaryPayments_FundId",
                table: "SalaryPayments");

            migrationBuilder.DropIndex(
                name: "IX_RoutineSlots_SessionId",
                table: "RoutineSlots");

            migrationBuilder.DropIndex(
                name: "IX_MessageLogs_RelatedNoticeId",
                table: "MessageLogs");

            migrationBuilder.DropIndex(
                name: "IX_LedgerEntries_DonorId",
                table: "LedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_LedgerEntries_FeeReceiptId",
                table: "LedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_LedgerEntries_SalaryPaymentId",
                table: "LedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_HostelLeaves_StudentId",
                table: "HostelLeaves");

            migrationBuilder.DropIndex(
                name: "IX_HostelAllocations_StudentId",
                table: "HostelAllocations");

            migrationBuilder.DropIndex(
                name: "IX_HifzDailyLogs_TeacherId",
                table: "HifzDailyLogs");

            migrationBuilder.DropIndex(
                name: "IX_FeeReceipts_StudentId_Date",
                table: "FeeReceipts");

            migrationBuilder.DropIndex(
                name: "IX_FeeReceiptLines_InvoiceId",
                table: "FeeReceiptLines");

            migrationBuilder.DropIndex(
                name: "IX_FeeInvoices_Month",
                table: "FeeInvoices");

            migrationBuilder.DropIndex(
                name: "IX_FeeInvoices_SessionId",
                table: "FeeInvoices");

            migrationBuilder.DropIndex(
                name: "IX_Exams_SessionId",
                table: "Exams");

            migrationBuilder.DropIndex(
                name: "IX_ExamResults_StudentId",
                table: "ExamResults");

            migrationBuilder.DropIndex(
                name: "IX_ExamMarks_StudentId",
                table: "ExamMarks");

            migrationBuilder.DropIndex(
                name: "IX_BookIssues_StaffId",
                table: "BookIssues");

            migrationBuilder.DropIndex(
                name: "IX_BookIssues_StudentId",
                table: "BookIssues");

            migrationBuilder.DropIndex(
                name: "IX_AdmissionApplications_SessionId",
                table: "AdmissionApplications");

            migrationBuilder.DropIndex(
                name: "IX_AdmissionApplications_StudentId",
                table: "AdmissionApplications");

            migrationBuilder.CreateIndex(
                name: "IX_FeeReceipts_StudentId",
                table: "FeeReceipts",
                column: "StudentId");
        }
    }
}
