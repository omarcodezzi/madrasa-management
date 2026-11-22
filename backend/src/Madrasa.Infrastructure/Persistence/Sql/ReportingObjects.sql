-- ============================================================================
-- MMS reporting objects: views, a function and stored procedures for reports,
-- Excel/Power BI, SMS tools and maintenance. The application itself uses EF Core;
-- these objects read the same tables and never change data (except the backup).
-- Each batch is separated by GO. Applied by the migration "ReportingObjects".
-- Codes: Gender 1=Male 2=Female · Student status 1=Active 2=Left 3=Graduated 4=Suspended
--        Attendance 1=Present 2=Absent 3=Late 4=Leave · Ledger direction 1=In 2=Out
-- ============================================================================

-- Current session id (the one marked IsCurrent)
CREATE OR ALTER FUNCTION dbo.fn_CurrentSessionId() RETURNS int
AS
BEGIN
    RETURN (SELECT TOP (1) Id FROM dbo.AcademicSessions WHERE IsCurrent = 1 ORDER BY StartDate DESC);
END
GO

-- Total unpaid fees of one student (invoice amount - discount - non-voided payments)
CREATE OR ALTER FUNCTION dbo.fn_StudentDue(@StudentId int) RETURNS decimal(12,2)
AS
BEGIN
    RETURN ISNULL((
        SELECT SUM(i.Amount - i.Discount - ISNULL(p.Paid, 0))
        FROM dbo.FeeInvoices i
        OUTER APPLY (SELECT SUM(l.Amount) AS Paid FROM dbo.FeeReceiptLines l
                     JOIN dbo.FeeReceipts r ON r.Id = l.ReceiptId AND r.IsVoided = 0
                     WHERE l.InvoiceId = i.Id) p
        WHERE i.StudentId = @StudentId AND i.Amount - i.Discount - ISNULL(p.Paid, 0) > 0), 0);
END
GO

-- Every student in the current session with class, section, roll and primary guardian
CREATE OR ALTER VIEW dbo.vw_CurrentStudents
AS
SELECT s.Id AS StudentId, s.StudentCode, s.Name, s.NameAr,
       CASE s.Gender WHEN 1 THEN N'ছাত্র' ELSE N'ছাত্রী' END AS Gender,
       s.DateOfBirth, s.FatherName, s.Phone, s.IsResidential, s.IsOrphan,
       CASE s.Status WHEN 1 THEN N'সক্রিয়' WHEN 2 THEN N'চলে গেছে' WHEN 3 THEN N'উত্তীর্ণ' ELSE N'স্থগিত' END AS Status,
       d.NameBn AS Department, c.Id AS ClassId, c.NameBn AS ClassName, sec.Name AS Section, e.Roll,
       COALESCE(e.MonthlyFeeOverride, c.MonthlyFee) AS MonthlyFee,
       g.Name AS GuardianName, g.Relation AS GuardianRelation, g.Phone AS GuardianPhone, s.AdmissionDate
FROM dbo.Students s
JOIN dbo.Enrollments e ON e.StudentId = s.Id AND e.SessionId = dbo.fn_CurrentSessionId()
JOIN dbo.Classes c ON c.Id = e.ClassId
JOIN dbo.Departments d ON d.Id = c.DepartmentId
LEFT JOIN dbo.Sections sec ON sec.Id = e.SectionId
OUTER APPLY (SELECT TOP (1) gg.* FROM dbo.StudentGuardians sg JOIN dbo.Guardians gg ON gg.Id = sg.GuardianId
             WHERE sg.StudentId = s.Id ORDER BY sg.IsPrimary DESC) g
WHERE s.IsDeleted = 0;
GO

-- Unpaid fee invoices of active students
CREATE OR ALTER VIEW dbo.vw_FeeDues
AS
SELECT i.Id AS InvoiceId, s.Id AS StudentId, s.StudentCode, s.Name, c.NameBn AS ClassName, h.NameBn AS FeeHead, i.Month,
       i.Amount, i.Discount, ISNULL(p.Paid, 0) AS Paid, i.Amount - i.Discount - ISNULL(p.Paid, 0) AS Due, i.DueDate
FROM dbo.FeeInvoices i
JOIN dbo.Students s ON s.Id = i.StudentId AND s.IsDeleted = 0 AND s.Status = 1
JOIN dbo.FeeHeads h ON h.Id = i.FeeHeadId
LEFT JOIN dbo.Enrollments e ON e.StudentId = s.Id AND e.SessionId = i.SessionId
LEFT JOIN dbo.Classes c ON c.Id = e.ClassId
OUTER APPLY (SELECT SUM(l.Amount) AS Paid FROM dbo.FeeReceiptLines l
             JOIN dbo.FeeReceipts r ON r.Id = l.ReceiptId AND r.IsVoided = 0 WHERE l.InvoiceId = i.Id) p
WHERE i.Amount - i.Discount - ISNULL(p.Paid, 0) > 0;
GO

-- Balance of each fund (General, Zakat, Sadaqah, Lillah)
CREATE OR ALTER VIEW dbo.vw_FundBalances
AS
SELECT f.Id AS FundId, f.Code, f.NameBn AS Fund, f.IsRestricted,
       ISNULL(SUM(CASE WHEN l.Direction = 1 THEN l.Amount END), 0) AS TotalIn,
       ISNULL(SUM(CASE WHEN l.Direction = 2 THEN l.Amount END), 0) AS TotalOut,
       ISNULL(SUM(CASE WHEN l.Direction = 1 THEN l.Amount ELSE -l.Amount END), 0) AS Balance
FROM dbo.Funds f LEFT JOIN dbo.LedgerEntries l ON l.FundId = f.Id
GROUP BY f.Id, f.Code, f.NameBn, f.IsRestricted;
GO

-- Income and expense per month and category
CREATE OR ALTER VIEW dbo.vw_MonthlyLedger
AS
SELECT CONVERT(char(7), l.Date, 126) AS Month, c.NameBn AS Category,
       CASE c.Kind WHEN 1 THEN N'আয়' ELSE N'ব্যয়' END AS Kind,
       SUM(CASE WHEN l.Direction = 1 THEN l.Amount ELSE 0 END) AS Income,
       SUM(CASE WHEN l.Direction = 2 THEN l.Amount ELSE 0 END) AS Expense,
       COUNT(*) AS Entries
FROM dbo.LedgerEntries l JOIN dbo.LedgerCategories c ON c.Id = l.CategoryId
GROUP BY CONVERT(char(7), l.Date, 126), c.NameBn, c.Kind;
GO

-- Day attendance per class: EXEC dbo.usp_DailyAttendance '2026-11-23'
CREATE OR ALTER PROCEDURE dbo.usp_DailyAttendance @Date date = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @Date = ISNULL(@Date, CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'Bangladesh Standard Time' AS date));
    SELECT v.ClassName, COUNT(*) AS Students,
           SUM(CASE WHEN a.Status = 1 THEN 1 ELSE 0 END) AS Present,
           SUM(CASE WHEN a.Status = 3 THEN 1 ELSE 0 END) AS Late,
           SUM(CASE WHEN a.Status = 2 THEN 1 ELSE 0 END) AS Absent,
           SUM(CASE WHEN a.Status = 4 THEN 1 ELSE 0 END) AS Leave,
           SUM(CASE WHEN a.Id IS NULL THEN 1 ELSE 0 END) AS NotMarked,
           CAST(100.0 * SUM(CASE WHEN a.Status IN (1, 3) THEN 1 ELSE 0 END) / NULLIF(SUM(CASE WHEN a.Id IS NOT NULL THEN 1 ELSE 0 END), 0) AS decimal(5,1)) AS PresentPercent
    FROM dbo.vw_CurrentStudents v
    LEFT JOIN dbo.StudentAttendance a ON a.StudentId = v.StudentId AND a.Date = @Date
    WHERE v.Status = N'সক্রিয়'
    GROUP BY v.ClassId, v.ClassName
    ORDER BY v.ClassId;
END
GO

-- One student's attendance between two dates, with totals in a second result set
CREATE OR ALTER PROCEDURE dbo.usp_StudentAttendance @StudentId int, @From date, @To date
AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.Date, CASE a.Status WHEN 1 THEN N'উপস্থিত' WHEN 2 THEN N'অনুপস্থিত' WHEN 3 THEN N'দেরি' ELSE N'ছুটি' END AS Status, a.CheckIn
    FROM dbo.StudentAttendance a WHERE a.StudentId = @StudentId AND a.Date BETWEEN @From AND @To ORDER BY a.Date;
    SELECT SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) AS Present, SUM(CASE WHEN Status = 3 THEN 1 ELSE 0 END) AS Late,
           SUM(CASE WHEN Status = 2 THEN 1 ELSE 0 END) AS Absent, SUM(CASE WHEN Status = 4 THEN 1 ELSE 0 END) AS Leave
    FROM dbo.StudentAttendance WHERE StudentId = @StudentId AND Date BETWEEN @From AND @To;
END
GO

-- Fee collection of a month by class and fee head: EXEC dbo.usp_MonthlyCollection '2026-11'
CREATE OR ALTER PROCEDURE dbo.usp_MonthlyCollection @Month char(7)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @From date = CAST(@Month + '-01' AS date);
    DECLARE @To date = EOMONTH(@From);
    SELECT ISNULL(c.NameBn, N'—') AS ClassName, h.NameBn AS FeeHead, COUNT(DISTINCT r.Id) AS Receipts, SUM(l.Amount) AS Collected
    FROM dbo.FeeReceipts r
    JOIN dbo.FeeReceiptLines l ON l.ReceiptId = r.Id
    JOIN dbo.FeeHeads h ON h.Id = l.FeeHeadId
    LEFT JOIN dbo.Enrollments e ON e.StudentId = r.StudentId AND e.SessionId = dbo.fn_CurrentSessionId()
    LEFT JOIN dbo.Classes c ON c.Id = e.ClassId
    WHERE r.IsVoided = 0 AND r.Date BETWEEN @From AND @To
    GROUP BY c.Id, c.NameBn, h.Id, h.NameBn
    ORDER BY c.Id, h.Id;
END
GO

-- Students who owe fees, optionally for one class: EXEC dbo.usp_DueList NULL
CREATE OR ALTER PROCEDURE dbo.usp_DueList @ClassId int = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT v.StudentCode, v.Name, v.ClassName, v.Roll, v.GuardianName, v.GuardianPhone,
           COUNT(d.InvoiceId) AS UnpaidItems, SUM(d.Due) AS TotalDue, MIN(d.Month) AS OldestMonth
    FROM dbo.vw_CurrentStudents v
    JOIN dbo.vw_FeeDues d ON d.StudentId = v.StudentId
    WHERE @ClassId IS NULL OR v.ClassId = @ClassId
    GROUP BY v.StudentId, v.StudentCode, v.Name, v.ClassId, v.ClassName, v.Roll, v.GuardianName, v.GuardianPhone
    ORDER BY v.ClassId, v.Roll;
END
GO

-- Ledger with opening and running balance: EXEC dbo.usp_LedgerStatement '2026-11-01', '2026-11-30', NULL
CREATE OR ALTER PROCEDURE dbo.usp_LedgerStatement @From date, @To date, @FundId int = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Opening decimal(14,2) = ISNULL((SELECT SUM(CASE WHEN Direction = 1 THEN Amount ELSE -Amount END)
        FROM dbo.LedgerEntries WHERE Date < @From AND (@FundId IS NULL OR FundId = @FundId)), 0);
    SELECT l.Date, l.VoucherNo, l.Description, c.NameBn AS Category, f.NameBn AS Fund,
           CASE WHEN l.Direction = 1 THEN l.Amount END AS Income,
           CASE WHEN l.Direction = 2 THEN l.Amount END AS Expense,
           @Opening + SUM(CASE WHEN l.Direction = 1 THEN l.Amount ELSE -l.Amount END)
               OVER (ORDER BY l.Date, l.Id ROWS UNBOUNDED PRECEDING) AS Balance
    FROM dbo.LedgerEntries l
    JOIN dbo.LedgerCategories c ON c.Id = l.CategoryId
    JOIN dbo.Funds f ON f.Id = l.FundId
    WHERE l.Date BETWEEN @From AND @To AND (@FundId IS NULL OR l.FundId = @FundId)
    ORDER BY l.Date, l.Id;
    SELECT @Opening AS Opening;
END
GO

-- Merit list of an exam, optionally one class: EXEC dbo.usp_ExamMeritList 1, NULL
CREATE OR ALTER PROCEDURE dbo.usp_ExamMeritList @ExamId int, @ClassId int = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.NameBn AS ClassName, x.MeritPosition, s.StudentCode, s.Name, e.Roll, x.Total, x.Average,
           g.NameBn AS Division, x.FailedSubjects
    FROM dbo.ExamResults x
    JOIN dbo.Exams ex ON ex.Id = x.ExamId
    JOIN dbo.Students s ON s.Id = x.StudentId
    JOIN dbo.Enrollments e ON e.StudentId = s.Id AND e.SessionId = ex.SessionId
    JOIN dbo.Classes c ON c.Id = e.ClassId
    LEFT JOIN dbo.GradeRules g ON g.Id = x.GradeRuleId
    WHERE x.ExamId = @ExamId AND (@ClassId IS NULL OR e.ClassId = @ClassId)
    ORDER BY c.Id, CASE WHEN x.MeritPosition IS NULL THEN 1 ELSE 0 END, x.MeritPosition, s.Name;
END
GO

-- Salary sheet of a month: who was paid and who is still unpaid: EXEC dbo.usp_SalarySheet '2026-11'
CREATE OR ALTER PROCEDURE dbo.usp_SalarySheet @Month char(7)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT st.StaffCode, st.Name, st.Designation, st.BasicSalary,
           p.Allowance, p.Deduction, p.NetPaid, p.PaidOn, f.NameBn AS Fund,
           CASE WHEN p.Id IS NULL THEN N'বাকি' ELSE N'দেওয়া হয়েছে' END AS State
    FROM dbo.Staff st
    LEFT JOIN dbo.SalaryPayments p ON p.StaffId = st.Id AND p.Month = @Month
    LEFT JOIN dbo.Funds f ON f.Id = p.FundId
    WHERE st.IsDeleted = 0 AND (st.IsActive = 1 OR p.Id IS NOT NULL)
    ORDER BY st.StaffCode;
END
GO

-- One-row summary for a quick look: EXEC dbo.usp_DashboardSummary
CREATE OR ALTER PROCEDURE dbo.usp_DashboardSummary
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Today date = CAST(SYSDATETIMEOFFSET() AT TIME ZONE 'Bangladesh Standard Time' AS date);
    DECLARE @MonthStart date = DATEFROMPARTS(YEAR(@Today), MONTH(@Today), 1);
    SELECT
        (SELECT COUNT(*) FROM dbo.vw_CurrentStudents WHERE Status = N'সক্রিয়') AS ActiveStudents,
        (SELECT COUNT(*) FROM dbo.StudentAttendance WHERE Date = @Today AND Status IN (1, 3)) AS PresentToday,
        (SELECT ISNULL(SUM(TotalPaid), 0) FROM dbo.FeeReceipts WHERE IsVoided = 0 AND Date >= @MonthStart) AS FeesThisMonth,
        (SELECT ISNULL(SUM(Due), 0) FROM dbo.vw_FeeDues) AS TotalDue,
        (SELECT ISNULL(SUM(Balance), 0) FROM dbo.vw_FundBalances) AS CashInFunds,
        (SELECT COUNT(*) FROM dbo.Staff WHERE IsDeleted = 0 AND IsActive = 1) AS ActiveStaff,
        (SELECT COUNT(*) FROM dbo.BookIssues WHERE ReturnedOn IS NULL AND DueOn < @Today) AS OverdueBooks;
END
GO

-- Full backup to a folder on the database server: EXEC dbo.usp_BackupDatabase N'/var/opt/mssql/backup'
CREATE OR ALTER PROCEDURE dbo.usp_BackupDatabase @Folder nvarchar(400)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Db sysname = DB_NAME();
    DECLARE @File nvarchar(600) = @Folder + N'/' + @Db + N'_' + FORMAT(SYSDATETIME(), 'yyyyMMdd-HHmm') + N'.bak';
    BACKUP DATABASE @Db TO DISK = @File WITH INIT, CHECKSUM;
    RESTORE VERIFYONLY FROM DISK = @File WITH CHECKSUM;
    SELECT @File AS BackupFile;
END
GO
