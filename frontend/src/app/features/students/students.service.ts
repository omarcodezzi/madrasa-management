import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { API, PagedResult } from '../../core/api';
import { StudentDues } from '../fees/fee.models';
import { StudentDetail, StudentInput, StudentMonth, StudentRow } from './student.models';

@Injectable({ providedIn: 'root' })
export class StudentsApi {
  private http = inject(HttpClient);
  private url = `${API}/students`;

  /** Madrasas have hundreds, not tens of thousands, of students: load the list once and filter in the browser. */
  all(params: Record<string, string | number> = {}) {
    return firstValueFrom(this.http.get<PagedResult<StudentRow>>(this.url, { params: { pageSize: 500, ...params } }));
  }
  get(id: number) { return firstValueFrom(this.http.get<StudentDetail>(`${this.url}/${id}`)); }
  nextCodes(classId?: number | null) {
    return firstValueFrom(this.http.get<{ studentCode: string; roll: number }>(`${this.url}/next-codes`, { params: classId ? { classId } : {} }));
  }
  create(input: StudentInput) { return firstValueFrom(this.http.post<StudentDetail>(this.url, input)); }
  update(id: number, input: StudentInput) { return firstValueFrom(this.http.put<StudentDetail>(`${this.url}/${id}`, input)); }
  remove(id: number) { return firstValueFrom(this.http.delete<void>(`${this.url}/${id}`)); }
  attendance(id: number, month?: string) { return firstValueFrom(this.http.get<StudentMonth>(`${this.url}/${id}/attendance`, { params: month ? { month } : {} })); }
  dues(id: number) { return firstValueFrom(this.http.get<StudentDues>(`${this.url}/${id}/dues`)); }
}
