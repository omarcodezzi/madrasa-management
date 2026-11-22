import { tr } from '../../core/tr';
export interface HifzStudentRow {
  studentId: number; studentCode: string; name: string; photoPath?: string | null; className: string; roll: number;
  completedJuz: number; currentJuz?: number | null; loggedToday: boolean; lastDate?: string | null;
  lastJuz?: number | null; lastFrom?: number | null; lastTo?: number | null; lastText?: string | null; avgMistakes: number;
}
export interface HifzOverview { date: string; total: number; loggedToday: number; students: HifzStudentRow[]; }
export type JuzStatus = 'None' | 'InProgress' | 'Completed';
export interface JuzCell { juzNo: number; status: JuzStatus; completedOn?: string | null; pagesDone: number; }
export interface HifzLog {
  id: number; date: string; sabakJuz?: number | null; sabakFromPage?: number | null; sabakToPage?: number | null; sabakText?: string | null;
  sabqiText?: string | null; manzilText?: string | null; mistakes: number; quality?: number | null; remarks?: string | null;
}
export interface WeekPoint { weekStart: string; pages: number; avgMistakes: number; days: number; }
export interface HifzStudentDetail {
  studentId: number; studentCode: string; name: string; photoPath?: string | null; className?: string | null; roll?: number | null;
  completedJuz: number; juz: JuzCell[]; logs: HifzLog[]; weekly: WeekPoint[]; today?: HifzLog | null;
}
export type HifzLogInput = Omit<HifzLog, 'id' | 'date'> & { date?: string | null };

export const PAGES_PER_JUZ = 20;
export const QUALITY_LABEL: Record<number, string> = { 5: tr('চমৎকার'), 4: tr('ভালো'), 3: tr('মোটামুটি'), 2: tr('দুর্বল'), 1: tr('আবার পড়বে') };
