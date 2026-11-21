import { Injectable, signal } from '@angular/core';

export type PrintMode = 'a4' | 'pos';
export type PrintJob =
  | { kind: 'receipt'; data: import('../../features/fees/fee.models').Receipt }
  | { kind: 'idcards'; data: import('../../features/students/student.models').StudentDetail[] }
  | { kind: 'admission'; data: import('../../features/students/student.models').StudentDetail };

/**
 * Renders a document into <app-print-host> and opens the browser print dialog.
 * A4 for forms and ID card sheets; POS for 80 mm receipt printers.
 */
@Injectable({ providedIn: 'root' })
export class PrintService {
  readonly job = signal<(PrintJob & { mode: PrintMode }) | null>(null);
  private style?: HTMLStyleElement;

  print(job: PrintJob, mode: PrintMode = 'a4') {
    this.job.set({ ...job, mode });
    this.style ??= document.head.appendChild(document.createElement('style'));
    this.style.textContent = mode === 'pos' ? '@page{size:80mm auto;margin:3mm}' : '@page{size:A4;margin:12mm}';
    document.body.classList.add('printing');
    const done = () => { document.body.classList.remove('printing'); window.removeEventListener('afterprint', done); };
    window.addEventListener('afterprint', done);
    // Wait for the document (and QR images) to render
    setTimeout(() => { try { window.print(); } catch { done(); } }, 350);
  }
}
