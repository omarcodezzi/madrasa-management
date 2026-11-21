import { ClassSubject } from '../academic/academic.models';

export interface ExamDto { id: number; name: string; startDate?: string | null; isPublished: boolean; marksEntered: number; marksExpected: number; }
export interface MarkCell { classSubjectId: number; marks?: number | null; isAbsent: boolean; }
export interface Grade { id: number; nameBn: string; nameAr?: string | null; minAverage: number; }
export interface ResultRow {
  studentId: number; studentCode: string; name: string; roll: number; cells: MarkCell[];
  total: number; average: number; failedSubjects: number; gradeId?: number | null; grade?: string | null; gradeAr?: string | null; merit?: number | null; complete: boolean;
}
export interface ResultSheet { examId: number; examName: string; isPublished: boolean; classId: number; className: string; subjects: ClassSubject[]; grades: Grade[]; rows: ResultRow[]; }
export interface SubjectResult { subject: string; subjectAr?: string | null; fullMarks: number; passMarks: number; marks?: number | null; isAbsent: boolean; }
export interface StudentExamResult {
  examId: number; examName: string; startDate?: string | null; isPublished: boolean; className?: string | null; subjects: SubjectResult[];
  total: number; average: number; failedSubjects: number; grade?: string | null; gradeAr?: string | null; merit?: number | null; classSize: number;
}
/** What the mark sheet print template needs. */
export interface MarkSheet { student: { name: string; studentCode: string; fatherName?: string | null; roll?: number | null }; result: StudentExamResult; }

/** Division tone: the lowest rule (রাসিব) is a fail. */
export function gradeTone(grade: string | null | undefined, grades: Grade[]): 'success' | 'info' | 'gold' | 'neutral' | 'danger' {
  if (!grade) return 'neutral';
  const sorted = [...grades].sort((a, b) => b.minAverage - a.minAverage);
  const i = sorted.findIndex((g) => g.nameBn === grade);
  if (i === sorted.length - 1) return 'danger';
  return i === 0 ? 'success' : i === 1 ? 'info' : 'gold';
}

export interface Computed { total: number; average: number; failed: number; grade: Grade | null; merit: number | null; complete: boolean; }

/**
 * Same rules as the API (ResultCalculator + merit): any failed or absent kitab → the lowest division;
 * merit only for complete, passing students, by total, ties share a place.
 */
export function computeResults(subjects: ClassSubject[], rows: { studentId: number; cells: MarkCell[] }[], grades: Grade[]): Map<number, Computed> {
  const rules = [...grades].sort((a, b) => b.minAverage - a.minAverage);
  const out = new Map<number, Computed>();
  for (const r of rows) {
    const cells = subjects.map((s) => r.cells.find((c) => c.classSubjectId === s.id) ?? { classSubjectId: s.id, marks: null, isAbsent: false });
    const complete = subjects.length > 0 && cells.every((c) => c.isAbsent || c.marks != null);
    const total = cells.reduce((a, c) => a + (c.isAbsent ? 0 : c.marks ?? 0), 0);
    const average = subjects.length ? Math.round((total / subjects.length) * 100) / 100 : 0;
    const failed = cells.filter((c, i) => c.isAbsent || (c.marks ?? 0) < subjects[i].passMarks).length;
    const grade = !complete ? null : failed ? rules.at(-1) ?? null : rules.find((g) => average >= g.minAverage) ?? null;
    out.set(r.studentId, { total, average, failed, grade, merit: null, complete });
  }
  const ranked = [...out.entries()].filter(([, c]) => c.complete && !c.failed).sort((a, b) => b[1].total - a[1].total);
  ranked.forEach(([id, c], i) => { c.merit = i > 0 && ranked[i - 1][1].total === c.total ? ranked[i - 1][1].merit : i + 1; out.set(id, c); });
  return out;
}
