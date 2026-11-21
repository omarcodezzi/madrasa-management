using Madrasa.Domain.Common;
using Madrasa.Domain.Enums;

namespace Madrasa.Domain.Entities;

public class HostelRoom : BaseEntity
{
    public string? Building { get; set; }
    public string RoomNo { get; set; } = "";
    public int Seats { get; set; }
    public Gender ForGender { get; set; }
}

public class HostelAllocation : BaseEntity
{
    public int StudentId { get; set; }
    public int RoomId { get; set; }
    public HostelRoom? Room { get; set; }
    public int SeatNo { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

public class HostelLeave : BaseEntity
{
    public int StudentId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? Reason { get; set; }
    public string? TakenBy { get; set; }
    public DateOnly? ReturnedOn { get; set; }
}

public class Book : BaseEntity
{
    public string Title { get; set; } = "";
    public string? TitleAr { get; set; }
    public string? Author { get; set; }
    public BookLanguage Language { get; set; }
    public string? Category { get; set; }
    public string? ShelfNo { get; set; }
    public int TotalCopies { get; set; } = 1;
}

public class BookIssue : BaseEntity
{
    public int BookId { get; set; }
    public Book? Book { get; set; }
    public int? StudentId { get; set; }
    public int? StaffId { get; set; }
    public DateOnly IssuedOn { get; set; }
    public DateOnly DueOn { get; set; }
    public DateOnly? ReturnedOn { get; set; }
    public decimal Fine { get; set; }
}

public class Notice : BaseEntity
{
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public DateOnly Date { get; set; }
    public NoticeAudience Audience { get; set; } = NoticeAudience.All;
    public bool IsPublic { get; set; } = true;
    public string? CreatedBy { get; set; }
}

public class SchoolEvent : BaseEntity
{
    public string Title { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public EventKind Kind { get; set; } = EventKind.General;
}

public class MessageLog
{
    public long Id { get; set; }
    public MessageChannel Channel { get; set; }
    public string ToPhone { get; set; } = "";
    public string Body { get; set; } = "";
    public MessageStatus Status { get; set; }
    public DateTime? SentAt { get; set; }
    public int? RelatedNoticeId { get; set; }
}

public class AuditLog
{
    public long Id { get; set; }
    public string? UserId { get; set; }
    public string Action { get; set; } = "";
    public string Entity { get; set; } = "";
    public string? EntityId { get; set; }
    public string? Changes { get; set; }
    public DateTime At { get; set; }
}
