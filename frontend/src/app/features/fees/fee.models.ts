import { PaymentMethod } from '../../core/api';

export interface FeeHead { id: number; nameBn: string; frequency: 'Once' | 'Monthly' | 'PerExam' | 'Yearly'; defaultAmount: number; }
export interface DueItem { invoiceId: number; feeHeadId: number; feeHead: string; month?: string | null; amount: number; discount: number; paid: number; due: number; }
export interface ReceiptSummary { id: number; receiptNo: number; date: string; studentId: number; studentName: string; className?: string; totalPaid: number; method: PaymentMethod; isVoided: boolean; months: string; }
export interface StudentDues {
  studentId: number; studentCode: string; name: string; className?: string; roll?: number; phone?: string; totalDue: number;
  items: DueItem[]; recentReceipts: ReceiptSummary[];
}
export interface ReceiptLine { feeHead: string; month?: string | null; amount: number; }
export interface Receipt {
  id: number; receiptNo: number; date: string; studentId: number; studentCode: string; studentName: string; fatherName?: string; className?: string; roll?: number;
  totalPaid: number; method: PaymentMethod; reference?: string; isVoided: boolean; collectedBy?: string; lines: ReceiptLine[]; remainingDue: number;
}
export interface DueListRow { studentId: number; studentCode: string; name: string; className?: string; roll?: number; phone?: string; totalDue: number; monthsDue: number; oldestMonth?: string; }
export interface MonthSummary { month: string; expected: number; collectedForMonth: number; collectedInMonth: number; paidStudents: number; dueStudents: number; }

export const METHOD_LABEL: Record<PaymentMethod, string> = { Cash: 'নগদ', BKash: 'বিকাশ', Nagad: 'নগদ (Nagad)', Bank: 'ব্যাংক' };
