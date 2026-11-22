import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { DatePipe, DigitsPipe, MoneyPipe, MonthPipe } from '../../core/format';
import { Badge } from '../../shared/ui';
import { METHOD_LABEL, Receipt } from './fee.models';
import { TrPipe } from '../../core/tr';

/** On-screen receipt (the printed copy comes from the print host). */
@Component({
  selector: 'app-receipt-view',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TrPipe, DatePipe, DigitsPipe, MoneyPipe, MonthPipe, Badge],
  template: `
    @let r = receipt();
    <div class="rounded-xl border border-dashed border-line p-4 bg-soft/40">
      <div class="flex justify-between gap-3 mb-3">
        <div><small class="text-mut">{{ 'রসিদ নং' | tr }}</small><b class="block font-serif text-xl">{{ pad(r.receiptNo) | digits }}</b></div>
        <div class="text-right"><small class="text-mut">{{ 'তারিখ' | tr }}</small><b class="block">{{ r.date | bdate }}</b></div>
      </div>
      @if (r.isVoided) { <m-badge tone="danger" [dot]="true" class="mb-2">{{ 'এই রসিদ বাতিল করা হয়েছে' | tr }}</m-badge> }
      <p class="m-0 mb-3"><b>{{ r.studentName }}</b> <span class="text-mut text-sm">· {{ r.studentCode }} · {{ r.className }} · {{ 'রোল {n}' | tr: { n: (r.roll | digits) } }}</span></p>
      <table class="w-full text-[15px] border-collapse">
        @for (l of r.lines; track $index) {
          <tr class="border-b border-line"><td class="py-2">{{ l.feeHead }}@if (l.month) { <span class="text-mut"> · {{ l.month | month }}</span> }</td><td class="py-2 text-right tabular">{{ l.amount | money }}</td></tr>
        }
        <tr><th class="pt-3 text-left">{{ 'মোট জমা' | tr }}</th><th class="pt-3 text-right font-serif text-xl">{{ r.totalPaid | money }}</th></tr>
        <tr class="text-sm text-mut"><td class="pt-1">{{ method[r.method] }}@if (r.reference) { · {{ r.reference }} }</td><td class="pt-1 text-right">{{ 'অবশিষ্ট বকেয়া {m}' | tr: { m: (r.remainingDue | money) } }}</td></tr>
      </table>
      @if (r.collectedBy) { <p class="m-0 mt-3 text-[13px] text-mut">{{ 'আদায় করেছেন: {name}' | tr: { name: r.collectedBy } }}</p> }
    </div>
  `,
})
export class ReceiptView {
  readonly receipt = input.required<Receipt>();
  protected method = METHOD_LABEL;
  protected pad = (n: number) => String(n).padStart(6, '0');
}
