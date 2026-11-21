import { Injectable, signal } from '@angular/core';

export type ToastKind = 'ok' | 'err' | 'info';
export interface Toast { id: number; text: string; kind: ToastKind; }

@Injectable({ providedIn: 'root' })
export class ToastService {
  private seq = 0;
  readonly toasts = signal<Toast[]>([]);

  show(text: string, kind: ToastKind = 'ok', ms = 3200) {
    const t = { id: ++this.seq, text, kind };
    this.toasts.update((l) => [...l, t]);
    setTimeout(() => this.dismiss(t.id), ms);
  }
  success(text: string) { this.show(text, 'ok'); }
  error(text: string) { this.show(text, 'err', 5000); }
  dismiss(id: number) { this.toasts.update((l) => l.filter((t) => t.id !== id)); }
}
