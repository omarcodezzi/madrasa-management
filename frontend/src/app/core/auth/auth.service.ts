import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { API } from '../api';

export interface AuthUser { id: string; userName: string; fullName: string; roles: string[]; permissions: string[]; language: string; }
interface LoginResponse { accessToken: string; expiresAt: string; user: AuthUser; }
interface Stored { token: string; expiresAt: string; user: AuthUser; }

const KEY = 'mms.auth';

/** Permission keys — keep in sync with backend Application/Auth/Permissions.cs */
export const P = {
  dashboard: 'dashboard.view', studentsView: 'students.view', studentsEdit: 'students.edit', admission: 'admission.manage',
  attendance: 'attendance.take', hifz: 'hifz.edit', examsMarks: 'exams.marks', examsPublish: 'exams.publish',
  fees: 'fees.collect', accounts: 'accounts.manage', staff: 'staff.manage', hostel: 'hostel.manage', library: 'library.manage',
  notices: 'notices.manage', reports: 'reports.view', settings: 'settings.manage', portal: 'portal.view',
} as const;

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private state = signal<Stored | null>(this.load());

  readonly user = computed(() => this.state()?.user ?? null);
  readonly token = computed(() => this.state()?.token ?? null);
  readonly isLoggedIn = computed(() => !!this.state());
  private perms = computed(() => new Set(this.user()?.permissions ?? []));

  can(perm: string | undefined | null): boolean {
    return !perm || this.perms().has(perm);
  }

  async login(userName: string, password: string): Promise<void> {
    const r = await firstValueFrom(this.http.post<LoginResponse>(`${API}/auth/login`, { userName, password }));
    this.save({ token: r.accessToken, expiresAt: r.expiresAt, user: r.user });
  }

  /** Re-reads roles/permissions from the server (e.g. after an admin changes them). */
  async refresh(): Promise<void> {
    if (!this.state()) return;
    const u = await firstValueFrom(this.http.get<AuthUser>(`${API}/auth/me`));
    this.save({ ...this.state()!, user: u });
  }

  /** The server ends every other session on a password change and returns a fresh token for this one. */
  async changePassword(currentPassword: string, newPassword: string): Promise<void> {
    const r = await firstValueFrom(this.http.post<LoginResponse>(`${API}/auth/change-password`, { currentPassword, newPassword }));
    if (r?.accessToken) this.save({ token: r.accessToken, expiresAt: r.expiresAt, user: r.user });
  }

  /** expired: the server ended the session (time-out, password or role change); the login page says so and returns here. */
  logout(redirect = true, expired = false) {
    this.state.set(null);
    try { localStorage.removeItem(KEY); } catch { /* ignore */ }
    if (!redirect) return;
    const here = this.router.url;
    this.router.navigate(['/login'], expired && here && !here.startsWith('/login') ? { queryParams: { expired: 1, returnUrl: here } } : {});
  }

  /** Home page for this user: staff → dashboard, students/guardians → portal. */
  homeUrl(): string {
    return this.can(P.dashboard) ? '/dashboard' : this.can(P.portal) ? '/portal' : '/dashboard';
  }

  private save(s: Stored) {
    this.state.set(s);
    try { localStorage.setItem(KEY, JSON.stringify(s)); } catch { /* ignore */ }
  }
  private load(): Stored | null {
    try {
      const s = JSON.parse(localStorage.getItem(KEY) || 'null') as Stored | null;
      return s && new Date(s.expiresAt) > new Date() ? s : null;
    } catch { return null; }
  }
}
