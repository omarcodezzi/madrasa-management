import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { API } from '../../core/api';
import { ClassInput, ClassSubject, ClassSubjectInput, DepartmentOverview, Period, Routine, SubjectDto, TeacherDto } from './academic.models';
import { trApi } from '../../core/tr';

@Injectable({ providedIn: 'root' })
export class AcademicApi {
  private http = inject(HttpClient);
  private url = `${API}/academic`;
  overview() { return firstValueFrom(this.http.get<DepartmentOverview[]>(`${this.url}/overview`)); }
  createClass(i: ClassInput) { return firstValueFrom(this.http.post<{ id: number }>(`${this.url}/classes`, i)); }
  updateClass(id: number, i: ClassInput) { return firstValueFrom(this.http.put<{ id: number }>(`${this.url}/classes/${id}`, i)); }
  deleteClass(id: number) { return firstValueFrom(this.http.delete<void>(`${this.url}/classes/${id}`)); }
  subjects() { return firstValueFrom(this.http.get<SubjectDto[]>(`${this.url}/subjects`)); }
  teachers() { return firstValueFrom(this.http.get<TeacherDto[]>(`${this.url}/teachers`)); }
  classSubjects(classId: number) { return firstValueFrom(this.http.get<ClassSubject[]>(`${this.url}/classes/${classId}/subjects`)); }
  saveClassSubject(classId: number, id: number | null, i: ClassSubjectInput) {
    return firstValueFrom(id ? this.http.put<ClassSubject>(`${this.url}/classes/${classId}/subjects/${id}`, i) : this.http.post<ClassSubject>(`${this.url}/classes/${classId}/subjects`, i));
  }
  removeClassSubject(classId: number, id: number) { return firstValueFrom(this.http.delete<void>(`${this.url}/classes/${classId}/subjects/${id}`)); }
  routine(classId: number) { return firstValueFrom(this.http.get<Routine>(`${this.url}/classes/${classId}/routine`)); }
  saveRoutine(classId: number, periods: Period[], slots: { day: number; periodNo: number; classSubjectId: number; room?: string | null }[]) {
    return firstValueFrom(this.http.put<Routine>(`${this.url}/classes/${classId}/routine`, { periods, slots }));
  }
}

/** Field errors from a 400 ProblemDetails, keyed as the API sends them. */
export function fieldErrors(e: unknown): Record<string, string> {
  const errs = ((e as { error?: { errors?: Record<string, string> } })?.error?.errors) ?? {};
  return Object.fromEntries(Object.entries(errs).map(([k, v]) => [k, trApi(v)]));
}
