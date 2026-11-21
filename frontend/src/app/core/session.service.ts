import { HttpClient } from '@angular/common/http';
import { computed, effect, inject, Injectable, signal } from '@angular/core';
import { API } from './api';
import { AuthService } from './auth/auth.service';

export interface AcademicSession { id: number; name: string; startDate: string; endDate: string; isCurrent: boolean; }
export interface MadrasaProfile { nameBn: string; nameEn?: string; nameAr?: string; address?: string; phone?: string; logoPath?: string; }

/** Madrasa profile + the academic session the user is working in (always visible in the top bar, spec 8.3). */
@Injectable({ providedIn: 'root' })
export class SessionService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);

  readonly profile = signal<MadrasaProfile | null>(null);
  readonly sessions = signal<AcademicSession[]>([]);
  private selectedId = signal<number | null>(Number(localStorage.getItem('mms.session')) || null);
  readonly current = computed(() => {
    const l = this.sessions();
    return l.find((s) => s.id === this.selectedId()) ?? l.find((s) => s.isCurrent) ?? l[0] ?? null;
  });

  constructor() {
    this.http.get<MadrasaProfile>(`${API}/meta/profile`).subscribe({ next: (p) => this.profile.set(p), error: () => {} });
    effect(() => {
      if (this.auth.isLoggedIn()) this.http.get<AcademicSession[]>(`${API}/meta/sessions`).subscribe({ next: (s) => this.sessions.set(s), error: () => {} });
      else this.sessions.set([]);
    });
  }

  select(id: number) {
    this.selectedId.set(id);
    try { localStorage.setItem('mms.session', String(id)); } catch { /* ignore */ }
  }
}
