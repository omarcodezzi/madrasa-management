import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { API, PagedResult, PaymentMethod } from '../../core/api';
import { DueListRow, FeeHead, MonthSummary, Receipt, ReceiptSummary, StudentDues } from './fee.models';

export interface CollectLine { invoiceId?: number | null; feeHeadId?: number | null; month?: string | null; amount: number; }

@Injectable({ providedIn: 'root' })
export class FeesApi {
  private http = inject(HttpClient);
  private url = `${API}/fees`;
  heads() { return firstValueFrom(this.http.get<FeeHead[]>(`${this.url}/heads`)); }
  student(id: number) { return firstValueFrom(this.http.get<StudentDues>(`${this.url}/students/${id}`)); }
  collect(body: { studentId: number; lines: CollectLine[]; method: PaymentMethod; reference?: string | null; date?: string | null }) {
    return firstValueFrom(this.http.post<Receipt>(`${this.url}/collect`, body));
  }
  receipts(month?: string, q?: string, page = 1, pageSize = 200) {
    const params: Record<string, string | number> = { page, pageSize };
    if (month) params['month'] = month;
    if (q) params['q'] = q;
    return firstValueFrom(this.http.get<PagedResult<ReceiptSummary>>(`${this.url}/receipts`, { params }));
  }
  receipt(no: number) { return firstValueFrom(this.http.get<Receipt>(`${this.url}/receipts/${no}`)); }
  void(no: number) { return firstValueFrom(this.http.post<Receipt>(`${this.url}/receipts/${no}/void`, {})); }
  dues(month?: string, classId?: number | null) {
    const params: Record<string, string | number> = {};
    if (month) params['month'] = month;
    if (classId) params['classId'] = classId;
    return firstValueFrom(this.http.get<DueListRow[]>(`${this.url}/dues`, { params }));
  }
  summary(month?: string) { return firstValueFrom(this.http.get<MonthSummary>(`${this.url}/summary`, { params: month ? { month } : {} })); }
}
