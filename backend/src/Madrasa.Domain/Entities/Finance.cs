using Madrasa.Domain.Common;
using Madrasa.Domain.Enums;

namespace Madrasa.Domain.Entities;

public class Fund : BaseEntity
{
    public string NameBn { get; set; } = "";
    public string Code { get; set; } = "";
    public bool IsRestricted { get; set; }
}

public class FeeHead : BaseEntity
{
    public string NameBn { get; set; } = "";
    public FeeFrequency Frequency { get; set; }
    public decimal DefaultAmount { get; set; }
    public bool IsActive { get; set; } = true;
}

public class StudentDiscount : BaseEntity
{
    public int StudentId { get; set; }
    public int FeeHeadId { get; set; }
    public DiscountKind Kind { get; set; }
    public decimal Value { get; set; }
    public string? Reason { get; set; }
    public int? SponsorFundId { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
}

public class FeeInvoice : BaseEntity
{
    public int StudentId { get; set; }
    public int SessionId { get; set; }
    public int FeeHeadId { get; set; }
    public FeeHead? FeeHead { get; set; }
    public string? Month { get; set; }
    public decimal Amount { get; set; }
    public decimal Discount { get; set; }
    public DateOnly? DueDate { get; set; }
}

public class FeeReceipt : BaseEntity
{
    public int ReceiptNo { get; set; }
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public DateOnly Date { get; set; }
    public decimal TotalPaid { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
    public string? Reference { get; set; }
    public string? CollectedBy { get; set; }
    public bool IsVoided { get; set; }
    public List<FeeReceiptLine> Lines { get; set; } = new();
}

public class FeeReceiptLine : BaseEntity
{
    public int ReceiptId { get; set; }
    public int? InvoiceId { get; set; }
    public int FeeHeadId { get; set; }
    public FeeHead? FeeHead { get; set; }
    public string? Month { get; set; }
    public decimal Amount { get; set; }
}

public class SalaryPayment : BaseEntity
{
    public int StaffId { get; set; }
    public Staff? Staff { get; set; }
    public string Month { get; set; } = "";
    public decimal Basic { get; set; }
    public decimal Allowance { get; set; }
    public decimal Deduction { get; set; }
    public decimal NetPaid { get; set; }
    public DateOnly PaidOn { get; set; }
    public int? FundId { get; set; }
}

public class Donor : BaseEntity
{
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

public class LedgerCategory : BaseEntity
{
    public string NameBn { get; set; } = "";
    public LedgerKind Kind { get; set; }
}

/// <summary>Every money movement posts here, tagged with a fund, so Zakat/Lillah/General never mix.</summary>
public class LedgerEntry
{
    public long Id { get; set; }
    public DateOnly Date { get; set; }
    public int FundId { get; set; }
    public Fund? Fund { get; set; }
    public int CategoryId { get; set; }
    public LedgerCategory? Category { get; set; }
    public Direction Direction { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public string? VoucherNo { get; set; }
    public int? DonorId { get; set; }
    public int? FeeReceiptId { get; set; }
    public int? SalaryPaymentId { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
