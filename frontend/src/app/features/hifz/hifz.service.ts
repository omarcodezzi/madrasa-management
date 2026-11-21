import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { API } from '../../core/api';
import { HifzLog, HifzLogInput, HifzOverview, HifzStudentDetail, JuzStatus } from './hifz.models';

@Injectable({ providedIn: 'root' })
export class HifzApi {
  private http = inject(HttpClient);
  private url = `${API}/hifz`;
  overview(date?: string) { return firstValueFrom(this.http.get<HifzOverview>(this.url, { params: date ? { date } : {} })); }
  student(id: number) { return firstValueFrom(this.http.get<HifzStudentDetail>(`${this.url}/students/${id}`)); }
  saveLog(id: number, input: HifzLogInput) { return firstValueFrom(this.http.post<HifzLog>(`${this.url}/students/${id}/logs`, input)); }
  deleteLog(id: number, logId: number) { return firstValueFrom(this.http.delete<void>(`${this.url}/students/${id}/logs/${logId}`)); }
  setJuz(id: number, juzNo: number, status: Exclude<JuzStatus, 'None'> | null) {
    return firstValueFrom(this.http.put<void>(`${this.url}/students/${id}/juz`, { juzNo, status }));
  }
}
