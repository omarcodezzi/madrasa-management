import { Injectable, signal } from '@angular/core';

export type PrintMode = 'a4' | 'a4l' | 'pos';
export type PrintJob =
  | { kind: 'receipt'; data: import('../../features/fees/fee.models').Receipt }
  | { kind: 'idcards'; data: import('../../features/students/student.models').StudentDetail[] }
  | { kind: 'admission'; data: import('../../features/students/student.models').StudentDetail }
  | { kind: 'tabulation'; data: import('../../features/exams/exam.models').ResultSheet }
  | { kind: 'marksheet'; data: import('../../features/exams/exam.models').MarkSheet[] }
  | { kind: 'voucher'; data: import('../../features/office/office.models').LedgerRow }
  | { kind: 'payslip'; data: import('../../features/office/office.models').SalaryRow[] }
  | { kind: 'report'; data: import('../../features/office/office.models').ReportTable };

/**
 * Renders a document into <app-print-host> and opens the browser print dialog.
 * A4 for forms and ID card sheets; POS for 80 mm receipt printers.
 */
@Injectable({ providedIn: 'root' })
export class PrintService {
  readonly job = signal<(PrintJob & { mode: PrintMode }) | null>(null);
  private style?: HTMLStyleElement;

  print(job: PrintJob, mode: PrintMode = 'a4') {
    this.style ??= document.head.appendChild(document.createElement('style'));
    // The tabulation sheet has a column per kitab, so it goes landscape
    if (job.kind === 'tabulation' || (job.kind === 'report' && job.data.columns.length > 7)) mode = 'a4l';
    this.job.set({ ...job, mode });
    this.style.textContent = mode === 'pos' ? '@page{size:80mm auto;margin:3mm}' : mode === 'a4l' ? '@page{size:A4 landscape;margin:10mm}' : '@page{size:A4;margin:12mm}';
    document.body.classList.add('printing');
    const done = () => { document.body.classList.remove('printing'); window.removeEventListener('afterprint', done); };
    window.addEventListener('afterprint', done);
    // Wait for the document (and QR images) to render
    setTimeout(() => { try { window.print(); } catch { done(); } }, 350);
  }
}
