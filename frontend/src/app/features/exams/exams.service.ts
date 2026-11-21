import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { API } from '../../core/api';
import { ExamDto, ResultSheet, StudentExamResult } from './exam.models';

@Injectable({ providedIn: 'root' })
export class ExamsApi {
  private http = inject(HttpClient);
  private url = `${API}/exams`;
  list() { return firstValueFrom(this.http.get<ExamDto[]>(this.url)); }
  save(id: number | null, body: { name: string; startDate?: string | null }) {
    return firstValueFrom(id ? this.http.put<ExamDto>(`${this.url}/${id}`, body) : this.http.post<ExamDto>(this.url, body));
  }
  remove(id: number) { return firstValueFrom(this.http.delete<void>(`${this.url}/${id}`)); }
  results(id: number, classId: number) { return firstValueFrom(this.http.get<ResultSheet>(`${this.url}/${id}/classes/${classId}/results`)); }
  saveMarks(id: number, classId: number, entries: { studentId: number; classSubjectId: number; marks: number | null; isAbsent: boolean }[]) {
    return firstValueFrom(this.http.put<{ changed: number }>(`${this.url}/${id}/marks`, { classId, entries }));
  }
  publish(id: number, value: boolean) { return firstValueFrom(this.http.post<ExamDto>(`${this.url}/${id}/publish`, {}, { params: { value } })); }
  student(studentId: number, examId?: number) {
    return firstValueFrom(this.http.get<StudentExamResult[]>(`${this.url}/students/${studentId}`, { params: examId ? { examId } : {} }));
  }
}
