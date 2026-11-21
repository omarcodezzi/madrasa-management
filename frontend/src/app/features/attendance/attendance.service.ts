import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { API, AttendanceStatus } from '../../core/api';
import { AttendanceDay, ScanResult } from './attendance.models';

@Injectable({ providedIn: 'root' })
export class AttendanceApi {
  private http = inject(HttpClient);
  private url = `${API}/attendance`;
  day(date: string, classId?: number | null) {
    return firstValueFrom(this.http.get<AttendanceDay>(`${this.url}/day`, { params: classId ? { date, classId } : { date } }));
  }
  mark(date: string, entries: { studentId: number; status: AttendanceStatus | null }[]) {
    return firstValueFrom(this.http.post<{ changed: number }>(`${this.url}/mark`, { date, entries }));
  }
  scan(code: string, date?: string) { return firstValueFrom(this.http.post<ScanResult>(`${this.url}/scan`, { code, date })); }
}

/** Short beep for scan feedback: high tone for success, low for a problem. */
export function beep(ok: boolean) {
  try {
    const ctx = new AudioContext();
    const o = ctx.createOscillator(); const g = ctx.createGain();
    o.frequency.value = ok ? 880 : 220; o.type = 'sine';
    g.gain.setValueAtTime(0.15, ctx.currentTime); g.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.18);
    o.connect(g).connect(ctx.destination); o.start(); o.stop(ctx.currentTime + 0.2);
    o.onended = () => ctx.close();
  } catch { /* no audio available */ }
}
