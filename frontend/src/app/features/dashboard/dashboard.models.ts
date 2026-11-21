import { DueListRow } from '../fees/fee.models';

export interface RollToken { studentId: number; roll: number; name: string; present: boolean; }
export interface RollClass { classId: number; className: string; total: number; present: number; students: RollToken[]; }
export interface Dashboard {
  today: string; isWorkDay: boolean; holidayTitle?: string;
  totalStudents: number; newThisMonth: number; presentToday: number; todayPercent: number; yesterdayPercent: number;
  collectedThisMonth: number; expectedThisMonth: number; collectedLastMonth: number; totalDue: number; dueStudents: number;
  rollCall: RollClass[]; attendanceTrend: { date: string; percent: number }[]; incomeExpense: { month: string; income: number; expense: number }[];
  byDepartment: { name: string; count: number }[]; recentAdmissions: { id: number; name: string; className?: string; admissionDate: string }[];
  topDues: DueListRow[]; notices: { id: number; title: string; date: string }[];
}
