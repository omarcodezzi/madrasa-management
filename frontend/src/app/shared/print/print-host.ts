import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { fileUrl } from '../../core/api';
import { DatePipe, DigitsPipe, MoneyPipe, MonthPipe } from '../../core/format';
import { IdCard } from '../../features/idcards/id-card';
import { METHOD_LABEL } from '../../features/fees/fee.models';
import { GENDER_LABEL } from '../../features/students/student.models';
import { DocHeader } from './doc-header';
import { PrintService } from './print.service';

@Component({
  selector: 'app-print-host',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DocHeader, IdCard, DigitsPipe, MoneyPipe, DatePipe, MonthPipe],
  template: `
    <div id="print-root" [class]="'pr-' + (job()?.mode ?? 'a4')">
      @if (job(); as j) {
        @switch (j.kind) {
          @case ('receipt') {
            @let r = $any(j.data);
            <div class="doc">
              <m-doc-header />
              <h2>বেতন রসিদ @if (r.isVoided) { (বাতিল) }</h2>
              <table>
                <tr><th>রসিদ নং</th><td>{{ pad(r.receiptNo) | digits }}</td><th>তারিখ</th><td>{{ r.date | bdate }}</td></tr>
                <tr><th>নাম</th><td colspan="3">{{ r.studentName }} ({{ r.studentCode }})</td></tr>
                <tr><th>শ্রেণি</th><td>{{ r.className }}</td><th>রোল</th><td>{{ r.roll | digits }}</td></tr>
              </table>
              <table>
                <tr><th>বিবরণ</th><th class="r">টাকা</th></tr>
                @for (l of r.lines; track $index) { <tr><td>{{ l.feeHead }}@if (l.month) { — {{ l.month | month }} }</td><td class="r">{{ l.amount | money }}</td></tr> }
                <tr><th>মোট জমা</th><th class="r">{{ r.totalPaid | money }}</th></tr>
                <tr><td>পরিশোধের মাধ্যম</td><td class="r">{{ method(r.method) }}@if (r.reference) { · {{ r.reference }} }</td></tr>
                <tr><th>অবশিষ্ট বকেয়া</th><th class="r">{{ r.remainingDue | money }}</th></tr>
              </table>
              <div class="signs"><span>আদায়কারী</span><span>হিসাবরক্ষক</span></div>
            </div>
          }
          @case ('idcards') {
            @for (page of pages(); track $index) {
              <div class="doc"><div class="sheet">
                @for (s of page; track s.id) { <app-id-card [s]="s" side="front" /><app-id-card [s]="s" side="back" /> }
              </div></div>
            }
          }
          @case ('admission') {
            @let s = $any(j.data);
            <div class="doc">
              <m-doc-header />
              <h2>ভর্তি ফরম</h2>
              <div style="display:flex; gap: 16px; align-items: flex-start">
                <table style="flex: 1">
                  <tr><th style="width:35%">আইডি</th><td>{{ s.studentCode }}</td></tr>
                  <tr><th>শিক্ষার্থীর নাম</th><td>{{ s.name }}</td></tr>
                  <tr><th>ছাত্র/ছাত্রী</th><td>{{ gender(s.gender) }}</td></tr>
                  <tr><th>জন্মতারিখ</th><td>{{ s.dateOfBirth | bdate }}</td></tr>
                  <tr><th>শ্রেণি</th><td>{{ s.className }}</td></tr>
                  <tr><th>রোল</th><td>{{ s.roll | digits }}</td></tr>
                  <tr><th>পিতার নাম</th><td>{{ s.fatherName }}</td></tr>
                  <tr><th>মাতার নাম</th><td>{{ s.motherName }}</td></tr>
                  @for (g of s.guardians; track $index) { <tr><th>অভিভাবক ({{ g.relation }})</th><td>{{ g.name }}, {{ g.phone | digits }}</td></tr> }
                  <tr><th>বর্তমান ঠিকানা</th><td>{{ s.presentAddress }}</td></tr>
                  <tr><th>স্থায়ী ঠিকানা</th><td>{{ s.permanentAddress }}</td></tr>
                  <tr><th>ভর্তির তারিখ</th><td>{{ s.admissionDate | bdate }}</td></tr>
                </table>
                @if (photo(s.photoPath)) { <img [src]="photo(s.photoPath)" alt="" style="width:30mm;height:35mm;object-fit:cover;border:1px solid #777"> }
              </div>
              <div class="signs"><span>অভিভাবকের স্বাক্ষর</span><span>মুহতামিম</span></div>
            </div>
          }
        }
      }
    </div>
  `,
})
export class PrintHost {
  private svc = inject(PrintService);
  protected job = this.svc.job;
  protected pages = computed(() => {
    const j = this.job();
    if (j?.kind !== 'idcards') return [];
    const out = [];
    for (let i = 0; i < j.data.length; i += 4) out.push(j.data.slice(i, i + 4));   // 4 cards (front + back) per A4
    return out;
  });
  protected pad = (n: number) => String(n).padStart(4, '0');
  protected method = (m: keyof typeof METHOD_LABEL) => METHOD_LABEL[m];
  protected gender = (g: keyof typeof GENDER_LABEL) => GENDER_LABEL[g];
  protected photo = (p?: string) => fileUrl(p);
}
