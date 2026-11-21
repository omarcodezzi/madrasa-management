export interface SectionDto { id: number; name: string; capacity?: number | null; studentCount: number; }
export interface ClassOverview { id: number; nameBn: string; nameAr?: string | null; departmentId: number; monthlyFee?: number | null; sortOrder: number; studentCount: number; subjectCount: number; sections: SectionDto[]; }
export interface DepartmentOverview { id: number; nameBn: string; nameAr?: string | null; kind: 'Nurani' | 'Nazera' | 'Hifz' | 'Kitab'; classes: ClassOverview[]; }
export interface ClassInput { departmentId: number; nameBn: string; nameAr?: string | null; monthlyFee?: number | null; sortOrder?: number | null; }
export interface SubjectDto { id: number; nameBn: string; nameAr?: string | null; code?: string | null; }
export interface TeacherDto { id: number; name: string; designation: string; phone?: string | null; }
export interface ClassSubject { id: number; subjectId: number; subjectName: string; subjectNameAr?: string | null; teacherId?: number | null; teacherName?: string | null; fullMarks: number; passMarks: number; }
export interface ClassSubjectInput { subjectId?: number | null; newSubjectName?: string | null; newSubjectNameAr?: string | null; teacherId?: number | null; fullMarks: number; passMarks: number; }
export interface Period { periodNo: number; start: string; end: string; }
export interface Slot { day: number; periodNo: number; classSubjectId: number; subjectName: string; teacherName?: string | null; room?: string | null; }
export interface Routine { classId: number; className: string; days: number[]; periods: Period[]; slots: Slot[]; }

/** 0 = Sunday … 6 = Saturday, as .NET DayOfWeek */
export const DAY_NAME = ['রবিবার', 'সোমবার', 'মঙ্গলবার', 'বুধবার', 'বৃহস্পতিবার', 'শুক্রবার', 'শনিবার'];
