using Madrasa.Domain.Enums;

namespace Madrasa.Application.Fees;

public record FeeHeadDto(int Id, string NameBn, FeeFrequency Frequency, decimal DefaultAmount);
public record DueItem(int InvoiceId, int FeeHeadId, string FeeHead, string? Month, decimal Amount, decimal Discount, decimal Paid, decimal Due);
public record StudentDues(int StudentId, string StudentCode, string Name, string? ClassName, int? Roll, string? Phone, decimal TotalDue, IReadOnlyList<DueItem> Items,
    IReadOnlyList<ReceiptSummary> RecentReceipts);
public record CollectLine(int? InvoiceId, int? FeeHeadId, string? Month, decimal Amount);
public record CollectRequest(int StudentId, IReadOnlyList<CollectLine> Lines, PaymentMethod Method, string? Reference, DateOnly? Date);
public record ReceiptLineDto(string FeeHead, string? Month, decimal Amount);
public record ReceiptDto(int Id, int ReceiptNo, DateOnly Date, int StudentId, string StudentCode, string StudentName, string? FatherName, string? ClassName, int? Roll,
    decimal TotalPaid, PaymentMethod Method, string? Reference, bool IsVoided, string? CollectedBy, IReadOnlyList<ReceiptLineDto> Lines, decimal RemainingDue);
public record ReceiptSummary(int Id, int ReceiptNo, DateOnly Date, int StudentId, string StudentName, string? ClassName, decimal TotalPaid, PaymentMethod Method, bool IsVoided, string Months);
public record DueListRow(int StudentId, string StudentCode, string Name, string? ClassName, int? Roll, string? Phone, decimal TotalDue, int MonthsDue, string? OldestMonth);
public record MonthSummary(string Month, decimal Expected, decimal CollectedForMonth, decimal CollectedInMonth, int PaidStudents, int DueStudents);
