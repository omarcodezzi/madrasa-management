import { describe, expect, it } from 'vitest';
import { computeResults, Grade } from './exam.models';

const grades: Grade[] = [
  { id: 1, nameBn: 'মুমতায', minAverage: 80 }, { id: 2, nameBn: 'জায়্যিদ জিদ্দান', minAverage: 65 },
  { id: 3, nameBn: 'জায়্যিদ', minAverage: 50 }, { id: 4, nameBn: 'মাকবুল', minAverage: 33 }, { id: 5, nameBn: 'রাসিব', minAverage: 0 },
];
const subjects = [1, 2].map((id) => ({ id, subjectId: id, subjectName: 'S' + id, fullMarks: 100, passMarks: 33 }));
const row = (studentId: number, a: number | null, b: number | null, absent = false) =>
  ({ studentId, cells: [{ classSubjectId: 1, marks: a, isAbsent: false }, { classSubjectId: 2, marks: b, isAbsent: absent }] });

describe('computeResults', () => {
  it('gives division by average and shares merit on ties', () => {
    const r = computeResults(subjects, [row(1, 90, 80), row(2, 70, 60), row(3, 60, 70), row(4, 50, 40)], grades);
    expect(r.get(1)!.grade!.nameBn).toBe('মুমতায');
    expect(r.get(2)!.grade!.nameBn).toBe('জায়্যিদ জিদ্দান');
    expect([r.get(1)!.merit, r.get(2)!.merit, r.get(3)!.merit, r.get(4)!.merit]).toEqual([1, 2, 2, 4]);
  });
  it('fails a student below pass marks or absent, with no merit', () => {
    const r = computeResults(subjects, [row(1, 95, 30), row(2, 95, null, true)], grades);
    expect(r.get(1)!.grade!.nameBn).toBe('রাসিব');
    expect(r.get(1)!.merit).toBeNull();
    expect(r.get(2)!.failed).toBe(1);
  });
  it('leaves incomplete students without a division', () => {
    const r = computeResults(subjects, [row(1, 95, null)], grades);
    expect(r.get(1)!.complete).toBe(false);
    expect(r.get(1)!.grade).toBeNull();
  });
});
