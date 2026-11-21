import { HttpClient } from '@angular/common/http';
import { computed, effect, inject, Injectable, signal } from '@angular/core';
import { API } from './api';
import { AuthService } from './auth/auth.service';

export interface ClassItem { id: number; nameBn: string; nameAr?: string; monthlyFee?: number; departmentId: number; departmentName: string; }
interface DeptDto { id: number; nameBn: string; classes: { id: number; nameBn: string; nameAr?: string; monthlyFee?: number }[]; }

/** Classes and departments, loaded once after sign-in. */
@Injectable({ providedIn: 'root' })
export class LookupsService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private depts = signal<DeptDto[]>([]);
  readonly classes = computed<ClassItem[]>(() =>
    this.depts().flatMap((d) => d.classes.map((c) => ({ ...c, departmentId: d.id, departmentName: d.nameBn }))),
  );
  readonly departments = computed(() => this.depts().map((d) => ({ id: d.id, nameBn: d.nameBn })));

  constructor() {
    effect(() => { if (this.auth.isLoggedIn()) this.reload(); });
  }
  reload() {
    this.http.get<DeptDto[]>(`${API}/meta/classes`).subscribe({ next: (d) => this.depts.set(d), error: () => {} });
  }
  className(id: number | null | undefined) { return this.classes().find((c) => c.id === id)?.nameBn ?? ''; }
}
