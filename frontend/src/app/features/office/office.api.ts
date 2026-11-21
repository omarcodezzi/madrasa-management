import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { API } from '../../core/api';
import { StudentMonth } from '../students/student.models';
import { StudentExamResult } from '../exams/exam.models';
import { StudentDues } from '../fees/fee.models';
import { HifzStudentDetail } from '../hifz/hifz.models';
import * as M from './office.models';

const params = (o: object) => {
  let p = new HttpParams();
  for (const [k, v] of Object.entries(o)) if (v !== undefined && v !== null && v !== '' && v !== 0) p = p.set(k, String(v));
  return p;
};

@Injectable({ providedIn: 'root' })
export class OfficeApi {
  private http = inject(HttpClient);
  private get<T>(u: string, q: object = {}) { return firstValueFrom(this.http.get<T>(`${API}/${u}`, { params: params(q) })); }
  private post<T>(u: string, b: unknown) { return firstValueFrom(this.http.post<T>(`${API}/${u}`, b)); }
  private put<T>(u: string, b: unknown) { return firstValueFrom(this.http.put<T>(`${API}/${u}`, b)); }
  private del(u: string) { return firstValueFrom(this.http.delete<void>(`${API}/${u}`)); }
  private save<T>(u: string, id: number | string | null, b: unknown) { return id ? this.put<T>(`${u}/${id}`, b) : this.post<T>(u, b); }

  // accounts
  summary(month: string) { return this.get<M.AccountsSummary>('accounts/summary', { month }); }
  categories() { return this.get<M.Category[]>('accounts/categories'); }
  ledger(f: M.LedgerFilter) { return this.get<M.LedgerPage>('accounts/ledger', f); }
  addEntry(i: M.EntryInput) { return this.post<M.LedgerRow>('accounts/entries', i); }
  deleteEntry(id: number) { return this.del(`accounts/entries/${id}`); }
  donors(q = '') { return this.get<M.Donor[]>('accounts/donors', { q }); }

  // staff
  staff(q = '') { return this.get<M.StaffRow[]>('staff', { q }); }
  staffOne(id: number) { return this.get<M.StaffDetail>(`staff/${id}`); }
  saveStaff(id: number | null, i: M.StaffInput) { return this.save<M.StaffDetail>('staff', id, i); }
  deleteStaff(id: number) { return this.del(`staff/${id}`); }
  staffDay(date: string) { return this.get<M.StaffDayRow[]>('staff/attendance', { date }); }
  markStaff(date: string, marks: { staffId: number; status: string | null }[]) { return this.post<{ changed: number }>('staff/attendance', { date, marks }); }
  salary(month: string) { return this.get<M.SalaryRow[]>('staff/salary', { month }); }
  pay(i: { staffId: number; month: string; allowance: number; deduction: number; fundId: number; paidOn: string | null }) { return this.post<M.SalaryRow>('staff/salary', i); }
  cancelPay(paymentId: number) { return this.del(`staff/salary/${paymentId}`); }

  // hostel
  hostel() { return this.get<M.HostelSummary>('hostel'); }
  saveRoom(id: number | null, i: { building: string | null; roomNo: string; seats: number; forGender: string }) { return this.save<M.Room>('hostel/rooms', id, i); }
  deleteRoom(id: number) { return this.del(`hostel/rooms/${id}`); }
  candidates(q = '') { return this.get<M.Resident[]>('hostel/candidates', { q }); }
  residents() { return this.get<M.Resident[]>('hostel/residents'); }
  allocate(i: { studentId: number; roomId: number; seatNo: number | null }) { return this.post<void>('hostel/allocations', i); }
  vacate(id: number) { return this.del(`hostel/allocations/${id}`); }
  leaves(open: boolean) { return this.get<M.LeaveRow[]>('hostel/leaves', { open }); }
  addLeave(i: { studentId: number; fromDate: string; toDate: string | null; reason: string | null; takenBy: string | null }) { return this.post<void>('hostel/leaves', i); }
  returned(id: number) { return this.post<void>(`hostel/leaves/${id}/return`, {}); }

  // library
  library() { return this.get<M.LibrarySummary>('library'); }
  books(q = '', language = '', category = '') { return this.get<M.Book[]>('library/books', { q, language, category }); }
  saveBook(id: number | null, i: M.BookInput) { return this.save<M.Book>('library/books', id, i); }
  deleteBook(id: number) { return this.del(`library/books/${id}`); }
  borrowers(q: string) { return this.get<M.Borrower[]>('library/borrowers', { q }); }
  issues(filter: string) { return this.get<M.Issue[]>('library/issues', { filter }); }
  issue(i: { bookId: number; studentId: number | null; staffId: number | null; days: number }) { return this.post<void>('library/issues', i); }
  returnBook(id: number, fine: number | null) { return this.post<M.Issue>(`library/issues/${id}/return`, { fine }); }

  // notices
  notices() { return this.get<M.Notice[]>('notices'); }
  saveNotice(id: number | null, i: M.NoticeInput) { return this.save<{ notice: M.Notice; smsQueued: number }>('notices', id, i); }
  deleteNotice(id: number) { return this.del(`notices/${id}`); }
  messages() { return this.get<M.MessageRow[]>('notices/messages'); }
  events(from?: string, to?: string) { return this.get<M.SchoolEvent[]>('notices/events', { from, to }); }
  saveEvent(id: number | null, i: Omit<M.SchoolEvent, 'id'>) { return this.save<M.SchoolEvent>('notices/events', id, i); }
  deleteEvent(id: number) { return this.del(`notices/events/${id}`); }

  // reports
  report(key: string, q: { month?: string; from?: string; to?: string; classId?: number; fundId?: number; examId?: number }) { return this.get<M.ReportTable>(`reports/${key}`, q); }

  // settings
  profile() { return this.get<M.Profile>('settings/profile'); }
  saveProfile(i: Omit<M.Profile, 'logoPath'> & { logo: string | null }) { return this.put<M.Profile>('settings/profile', i); }
  sessions() { return this.get<M.Session[]>('settings/sessions'); }
  saveSession(id: number | null, i: { name: string; startDate: string; endDate: string }) { return this.save<M.Session>('settings/sessions', id, i); }
  setCurrentSession(id: number) { return this.post<void>(`settings/sessions/${id}/current`, {}); }
  feeHeads() { return this.get<M.FeeHeadRow[]>('settings/fee-heads'); }
  saveFeeHead(id: number | null, i: { nameBn: string; frequency: string; defaultAmount: number; isActive: boolean }) { return this.save<M.FeeHeadRow>('settings/fee-heads', id, i); }
  grades() { return this.get<M.GradeRow[]>('settings/grades'); }
  saveGrades(i: { nameBn: string; nameAr: string | null; minAverage: number }[]) { return this.put<M.GradeRow[]>('settings/grades', i); }
  holidays(year: number) { return this.get<M.Holiday[]>('settings/holidays', { year }); }
  addHoliday(i: { date: string; title: string }) { return this.post<M.Holiday>('settings/holidays', i); }
  deleteHoliday(id: number) { return this.del(`settings/holidays/${id}`); }
  general() { return this.get<M.GeneralSettings>('settings/general'); }
  saveGeneral(i: M.GeneralSettings) { return this.put<M.GeneralSettings>('settings/general', i); }
  users() { return this.get<M.UserRow[]>('settings/users'); }
  saveUser(id: string | null, i: M.UserInput) { return this.save<M.UserRow>('settings/users', id, i); }
  resetPassword(id: string, password: string) { return this.post<void>(`settings/users/${id}/password`, { password }); }
  roles() { return this.get<M.RoleRow[]>('settings/roles'); }
  setRole(name: string, permissions: string[]) { return this.put<M.RoleRow>(`settings/roles/${name}`, { permissions }); }

  // admissions
  applications(stage = '', q = '') { return this.get<M.Application[]>('admissions/applications', { stage, q }); }
  pipeline() { return this.get<{ stage: M.Stage; count: number }[]>('admissions/pipeline'); }
  createApplication(i: M.ApplyInput) { return this.post<M.Application>('admissions/applications', i); }
  setStage(id: number, i: { stage: M.Stage; testMarks: number | null; remarks: string | null }) { return this.put<M.Application>(`admissions/applications/${id}/stage`, i); }
  admitted(id: number, studentId: number) { return this.put<M.Application>(`admissions/applications/${id}/admitted`, { studentId }); }

  // public site (no sign-in)
  publicInfo() { return this.get<M.PublicInfo>('public/info'); }
  apply(i: M.ApplyInput) { return this.post<{ applicationNo: string; message: string }>('public/apply', i); }
  status(no: string, phone: string) { return this.get<M.ApplicationStatus>('public/status', { no, phone }); }

  // portal
  portal() { return this.get<M.PortalHome>('portal'); }
  portalStudent(id: number, month: string) {
    return this.get<{ student: M.PortalChild; attendance: StudentMonth; dues: StudentDues; results: StudentExamResult[]; hifz: HifzStudentDetail | null }>(`portal/students/${id}`, { month });
  }
}
