import { AttendanceStatus, Gender } from '../../core/api';

// ---- accounts ----
export type Direction = 'In' | 'Out';
export type LedgerKind = 'Income' | 'Expense';
export interface FundBalance { id: number; nameBn: string; code: string; isRestricted: boolean; in: number; out: number; balance: number; inThisMonth: number; outThisMonth: number; }
export interface Category { id: number; nameBn: string; kind: LedgerKind; }
export interface LedgerRow { id: number; date: string; fundId: number; fund: string; categoryId: number; category: string; kind: LedgerKind; direction: Direction; amount: number;
  description: string | null; voucherNo: string | null; donor: string | null; donorPhone: string | null; source: 'Fee' | 'Salary' | 'Library' | 'Manual'; canDelete: boolean; }
export interface LedgerPage { items: LedgerRow[]; total: number; page: number; pageSize: number; totalIn: number; totalOut: number; }
export interface LedgerFilter { from?: string; to?: string; fundId?: number; categoryId?: number; direction?: Direction; q?: string; page?: number; pageSize?: number; }
export interface EntryInput { date: string; fundId: number; categoryId: number; amount: number; description: string | null; donorName: string | null; donorPhone: string | null; donorAddress: string | null; }
export interface Donor { id: number; name: string; phone: string | null; address: string | null; total: number; count: number; last: string | null; }
export interface CategoryTotal { category: string; kind: LedgerKind; amount: number; }
export interface AccountsSummary { month: string; income: number; expense: number; todayIn: number; todayOut: number; funds: FundBalance[]; byCategory: CategoryTotal[]; }

// ---- staff ----
export interface StaffRow { id: number; staffCode: string; name: string; designation: string; phone: string | null; photoPath: string | null; isActive: boolean;
  basicSalary: number | null; joinDate: string | null; subjects: number; today: AttendanceStatus | null; }
export interface SalaryRow { paymentId: number | null; staffId: number; staffCode: string; name: string; designation: string; month: string; basic: number; allowance: number;
  deduction: number; netPaid: number; paidOn: string | null; fund: string | null; present: number; workDays: number; }
export interface StaffDetail { id: number; staffCode: string; name: string; designation: string; phone: string | null; email: string | null; nid: string | null; address: string | null;
  qualification: string | null; joinDate: string | null; basicSalary: number | null; photoPath: string | null; isActive: boolean;
  subjects: { classSubjectId: number; className: string; subjectName: string }[]; presentThisMonth: number; workDaysThisMonth: number; salaries: SalaryRow[]; }
export interface StaffInput { name: string; designation: string; phone: string | null; email: string | null; nid: string | null; address: string | null; qualification: string | null;
  joinDate: string | null; basicSalary: number | null; isActive: boolean; photo: string | null; }
export interface StaffDayRow { staffId: number; name: string; designation: string; photoPath: string | null; status: AttendanceStatus | null; checkIn: string | null; }

// ---- hostel ----
export interface Occupant { allocationId: number; studentId: number; studentCode: string; name: string; photoPath: string | null; className: string | null; seatNo: number; fromDate: string; onLeave: boolean; }
export interface Room { id: number; building: string | null; roomNo: string; seats: number; forGender: Gender; occupants: Occupant[]; }
export interface HostelSummary { rooms: number; seats: number; occupied: number; onLeave: number; overdue: number; roomList: Room[]; }
export interface Resident { studentId: number; studentCode: string; name: string; className: string | null; gender: Gender; roomNo: string | null; }
export interface LeaveRow { id: number; studentId: number; studentCode: string; name: string; className: string | null; roomNo: string | null; fromDate: string; toDate: string | null;
  reason: string | null; takenBy: string | null; returnedOn: string | null; overdue: boolean; }

// ---- library ----
export type BookLanguage = 'Arabic' | 'Bangla' | 'Urdu' | 'English';
export const LANG_LABEL: Record<BookLanguage, string> = { Arabic: 'আরবি', Bangla: 'বাংলা', Urdu: 'উর্দু', English: 'ইংরেজি' };
export interface Book { id: number; title: string; titleAr: string | null; author: string | null; language: BookLanguage; category: string | null; shelfNo: string | null; totalCopies: number; available: number; }
export interface BookInput { title: string; titleAr: string | null; author: string | null; language: BookLanguage; category: string | null; shelfNo: string | null; totalCopies: number; }
export interface Issue { id: number; bookId: number; title: string; titleAr: string | null; borrowerKind: 'student' | 'staff'; borrowerId: number; borrowerCode: string; borrowerName: string;
  issuedOn: string; dueOn: string; returnedOn: string | null; fine: number; lateDays: number; fineDue: number;
  finePaid: number; fineWaived: number; fineOutstanding: number; }
export interface Borrower { kind: 'student' | 'staff'; id: number; code: string; name: string; detail: string | null; }
export interface LibrarySummary { titles: number; copies: number; out: number; overdue: number; finePerDay: number; categories: string[]; finesPending: number; fineOutstanding: number; }

// ---- notices ----
export type Audience = 'All' | 'Students' | 'Guardians' | 'Staff';
export const AUDIENCE_LABEL: Record<Audience, string> = { All: 'সবার জন্য', Students: 'শিক্ষার্থী', Guardians: 'অভিভাবক', Staff: 'উস্তাদ ও স্টাফ' };
export type EventKind = 'General' | 'Exam' | 'Holiday' | 'Meeting';
export const EVENT_LABEL: Record<EventKind, string> = { General: 'অনুষ্ঠান', Exam: 'পরীক্ষা', Holiday: 'ছুটি', Meeting: 'সভা' };
export interface Notice { id: number; title: string; body: string; date: string; audience: Audience; isPublic: boolean; createdBy: string | null; smsQueued: number; }
export interface NoticeInput { title: string; body: string; date: string | null; audience: Audience; isPublic: boolean; sendSms: boolean; }
export interface SchoolEvent { id: number; title: string; startDate: string; endDate: string | null; kind: EventKind; }
export interface MessageRow { id: number; toPhone: string; body: string; status: 'Queued' | 'Sent' | 'Failed'; sentAt: string | null; noticeId: number | null; }

// ---- reports ----
export type ColKind = 'text' | 'num' | 'money' | 'pct' | 'date' | 'month';
export interface ReportColumn { key: string; label: string; kind: ColKind; }
export interface ReportTable { key: string; title: string; subtitle: string; columns: ReportColumn[]; rows: Record<string, unknown>[]; totals: Record<string, unknown> | null; }

// ---- settings ----
export type FeeFrequency = 'Once' | 'Monthly' | 'PerExam' | 'Yearly';
export const FREQ_LABEL: Record<FeeFrequency, string> = { Once: 'একবার', Monthly: 'মাসিক', PerExam: 'প্রতি পরীক্ষায়', Yearly: 'বাৎসরিক' };
export interface Profile { nameBn: string; nameEn: string | null; nameAr: string | null; address: string | null; phone: string | null; email: string | null; logoPath: string | null; establishedYear: number | null; studentIdPrefix: string; }
export interface Session { id: number; name: string; startDate: string; endDate: string; isCurrent: boolean; students: number; }
export interface FeeHeadRow { id: number; nameBn: string; frequency: FeeFrequency; defaultAmount: number; isActive: boolean; inUse: boolean; }
export interface GradeRow { id: number; nameBn: string; nameAr: string | null; minAverage: number; sortOrder: number; }
export interface Holiday { id: number; date: string; title: string; }
export interface GeneralSettings { weeklyHoliday: number; libraryFinePerDay: number; onlineAdmission: boolean; }
export interface UserRow { id: string; userName: string; fullName: string; roles: string[]; isActive: boolean; lockedOut: boolean; staffId: number | null; studentId: number | null; guardianId: number | null; linkedName: string | null; }
export interface UserInput { userName: string; fullName: string; password: string | null; roles: string[]; isActive: boolean; staffId: number | null; studentId: number | null; guardianId: number | null; }
export interface RoleRow { name: string; permissions: string[]; users: number; locked: boolean; }

// ---- admissions & public ----
export type Stage = 'Applied' | 'Test' | 'Approved' | 'Admitted' | 'Rejected';
export const STAGE_LABEL: Record<Stage, string> = { Applied: 'আবেদন', Test: 'ভর্তি পরীক্ষা', Approved: 'অনুমোদিত', Admitted: 'ভর্তি হয়েছে', Rejected: 'বাতিল' };
export interface Application { id: number; applicationNo: string; applicantName: string; gender: Gender; dateOfBirth: string | null; fatherName: string | null; guardianPhone: string;
  address: string | null; previousInstitution: string | null; photoPath: string | null; classId: number; className: string; source: 'Office' | 'Online'; stage: Stage;
  testMarks: number | null; remarks: string | null; studentId: number | null; appliedAt: string; }
export interface ApplyInput { classId: number; applicantName: string; gender: Gender; dateOfBirth: string | null; fatherName: string | null; guardianPhone: string;
  address: string | null; previousInstitution: string | null; photo: string | null; website?: string; }
export interface PublicInfo { nameBn: string; nameEn: string | null; nameAr: string | null; address: string | null; phone: string | null; email: string | null; logoPath: string | null;
  establishedYear: number | null; students: number; teachers: number; huffaz: number; onlineAdmission: boolean; sessionName: string | null;
  classes: { id: number; nameBn: string; nameAr: string | null; department: string; monthlyFee: number | null }[]; notices: Notice[]; events: SchoolEvent[]; }
export interface ApplicationStatus { applicationNo: string; applicantName: string; className: string; stage: Stage; appliedAt: string; remarks: string | null; }

// ---- portal ----
export interface PortalChild { studentId: number; studentCode: string; name: string; photoPath: string | null; className: string | null; roll: number | null; isHifz: boolean; }
export interface PortalHome { kind: 'guardian' | 'student'; children: PortalChild[]; notices: Notice[]; }
