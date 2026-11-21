namespace Madrasa.Domain.Enums;

public enum Gender : byte { Male = 1, Female = 2 }
public enum DepartmentKind : byte { Nurani = 1, Nazera = 2, Hifz = 3, Kitab = 4 }
public enum StudentStatus : byte { Active = 1, Left = 2, Graduated = 3, Suspended = 4 }
public enum AttendanceStatus : byte { Present = 1, Absent = 2, Late = 3, Leave = 4 }
public enum AttendanceMethod : byte { Manual = 1, Qr = 2 }
public enum AdmissionStage : byte { Applied = 1, Test = 2, Approved = 3, Admitted = 4, Rejected = 5 }
public enum AdmissionSource : byte { Office = 1, Online = 2 }
public enum JuzStatus : byte { InProgress = 1, Completed = 2 }
public enum FeeFrequency : byte { Once = 1, Monthly = 2, PerExam = 3, Yearly = 4 }
public enum DiscountKind : byte { Percent = 1, Fixed = 2 }
public enum PaymentMethod : byte { Cash = 1, BKash = 2, Nagad = 3, Bank = 4 }
public enum LedgerKind : byte { Income = 1, Expense = 2 }
public enum Direction : byte { In = 1, Out = 2 }
public enum BookLanguage : byte { Arabic = 1, Bangla = 2, Urdu = 3, English = 4 }
public enum NoticeAudience : byte { All = 1, Students = 2, Guardians = 3, Staff = 4 }
public enum EventKind : byte { General = 1, Exam = 2, Holiday = 3, Meeting = 4 }
public enum MessageChannel : byte { Sms = 1, WhatsApp = 2 }
public enum MessageStatus : byte { Queued = 1, Sent = 2, Failed = 3 }

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Muhtamim = "Muhtamim";
    public const string Office = "Office";
    public const string Teacher = "Teacher";
    public const string Accountant = "Accountant";
    public const string HostelSupervisor = "HostelSupervisor";
    public const string Librarian = "Librarian";
    public const string Student = "Student";
    public const string Guardian = "Guardian";
    public static readonly string[] All =
        { SuperAdmin, Muhtamim, Office, Teacher, Accountant, HostelSupervisor, Librarian, Student, Guardian };
}
