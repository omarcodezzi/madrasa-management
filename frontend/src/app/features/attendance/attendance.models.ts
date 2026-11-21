import { AttendanceStatus } from '../../core/api';

export interface AttendanceRow {
  studentId: number; studentCode: string; name: string; photoPath?: string; classId: number; className: string; roll: number;
  status?: AttendanceStatus | null; checkIn?: string | null; method?: 'Manual' | 'Qr' | null; phone?: string | null;
}
export interface ClassTally { classId: number; className: string; total: number; present: number; late: number; absent: number; leave: number; }
export interface AttendanceDay { date: string; isWorkDay: boolean; holidayTitle?: string; rows: AttendanceRow[]; classes: ClassTally[]; }
export interface ScanResult { ok: boolean; alreadyMarked: boolean; message: string; studentId?: number; name?: string; className?: string; roll?: number; photoPath?: string; checkIn?: string; }

export const ATT_LABEL: Record<AttendanceStatus, string> = { Present: 'উপস্থিত', Absent: 'অনুপস্থিত', Late: 'দেরি', Leave: 'ছুটি' };
export const ATT_TONE = { Present: 'success', Absent: 'danger', Late: 'warning', Leave: 'info' } as const;
