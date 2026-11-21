import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { fileUrl } from '../../core/api';
import { PreferencesService } from '../../core/preferences.service';
import { DatePipe, DigitsPipe, MoneyPipe, MonthPipe, reportCell, todayIso } from '../../core/format';
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
          @case ('tabulation') {
            @let t = $any(j.data);
            <div class="doc">
              <m-doc-header />
              <h2>টেবুলেশন শিট · {{ t.examName }} · {{ t.className }}</h2>
              <table class="tab">
                <tr><th>মেধা</th><th>রোল</th><th style="text-align:left">নাম</th>
                  @for (s of t.subjects; track s.id) { <th>{{ s.subjectName }}<br><small>{{ s.fullMarks | digits }}</small></th> }
                  <th>মোট</th><th>গড়</th><th>বিভাগ</th></tr>
                @for (r of t.rows; track r.studentId) {
                  <tr><td class="c">{{ r.merit ? (r.merit | digits) : '—' }}</td><td class="c">{{ r.roll | digits }}</td><td>{{ r.name }}</td>
                    @for (c of r.cells; track c.classSubjectId; let i = $index) { <td class="c" [class.fail]="c.isAbsent || (c.marks ?? 0) < t.subjects[i].passMarks">{{ c.isAbsent ? 'অনু' : c.marks == null ? '' : (c.marks | digits) }}</td> }
                    <td class="c"><b>{{ r.total | digits }}</b></td><td class="c">{{ r.average | digits }}</td><td>{{ r.grade || 'অসম্পূর্ণ' }}</td></tr>
                }
              </table>
              <div class="signs"><span>পরীক্ষা নিয়ন্ত্রক</span><span>নাযেমে তা'লীমাত</span><span>মুহতামিম</span></div>
            </div>
          }
          @case ('marksheet') {
            @for (m of $any(j.data); track $index) {
              <div class="doc">
                <m-doc-header />
                <h2>মার্কশিট · {{ m.result.examName }}</h2>
                <table>
                  <tr><th style="width:20%">নাম</th><td>{{ m.student.name }}</td><th style="width:16%">আইডি</th><td>{{ m.student.studentCode }}</td></tr>
                  <tr><th>শ্রেণি</th><td>{{ m.result.className }}</td><th>রোল</th><td>{{ m.student.roll | digits }}</td></tr>
                  @if (m.student.fatherName) { <tr><th>পিতার নাম</th><td colspan="3">{{ m.student.fatherName }}</td></tr> }
                </table>
                <table>
                  <tr><th style="text-align:left">কিতাব / বিষয়</th><th>পূর্ণমান</th><th>পাস নম্বর</th><th>প্রাপ্ত নম্বর</th></tr>
                  @for (s of m.result.subjects; track $index) {
                    <tr><td>{{ s.subject }}@if (s.subjectAr) { <span class="ar"> ({{ s.subjectAr }})</span> }</td><td class="c">{{ s.fullMarks | digits }}</td><td class="c">{{ s.passMarks | digits }}</td>
                      <td class="c" [class.fail]="s.isAbsent || (s.marks ?? 0) < s.passMarks"><b>{{ s.isAbsent ? 'অনুপস্থিত' : s.marks == null ? '—' : (s.marks | digits) }}</b></td></tr>
                  }
                  <tr><th style="text-align:left">মোট</th><td></td><td></td><td class="c"><b>{{ m.result.total | digits }}</b></td></tr>
                </table>
                <table>
                  <tr><th>গড় নম্বর</th><td class="c">{{ m.result.average | digits }}</td><th>বিভাগ</th><td class="c"><b>{{ m.result.grade || 'অসম্পূর্ণ' }}</b>@if (m.result.gradeAr) { <span class="ar"> ({{ m.result.gradeAr }})</span> }</td></tr>
                  <tr><th>মেধা স্থান</th><td class="c">{{ m.result.merit ? (m.result.merit | digits) : '—' }}</td><th>শ্রেণির শিক্ষার্থী</th><td class="c">{{ m.result.classSize | digits }} জন</td></tr>
                </table>
                <div class="signs"><span>শ্রেণি উস্তাদ</span><span>পরীক্ষা নিয়ন্ত্রক</span><span>মুহতামিম</span></div>
              </div>
            }
          }
          @case ('voucher') {
            @let v = $any(j.data);
            <div class="doc">
              <m-doc-header />
              <h2>{{ v.direction === 'In' ? 'দানের রসিদ' : 'খরচের ভাউচার' }}</h2>
              <table>
                <tr><th style="width:22%">ভাউচার নং</th><td>{{ v.voucherNo }}</td><th style="width:16%">তারিখ</th><td>{{ v.date | bdate }}</td></tr>
                @if (v.donor) { <tr><th>দাতা</th><td colspan="3">{{ v.donor }}@if (v.donorPhone) { · {{ v.donorPhone | digits }} }</td></tr> }
                <tr><th>খাত</th><td>{{ v.category }}</td><th>ফান্ড</th><td>{{ v.fund }}</td></tr>
                @if (v.description) { <tr><th>বিবরণ</th><td colspan="3">{{ v.description }}</td></tr> }
                <tr><th>টাকার পরিমাণ</th><td colspan="3"><b>{{ v.amount | money }}</b></td></tr>
              </table>
              @if (v.direction === 'In') { <p style="text-align:center;font-size:11pt">জাযাকাল্লাহু খাইরান। আপনার দান মাদরাসার {{ v.fund }}-এ জমা হয়েছে।</p> }
              <div class="signs"><span>গ্রহণকারী</span><span>হিসাবরক্ষক</span><span>মুহতামিম</span></div>
            </div>
          }
          @case ('payslip') {
            @for (p of $any(j.data); track p.staffId) {
              <div class="doc">
                <m-doc-header />
                <h2>বেতন স্লিপ · {{ p.month | month }}</h2>
                <table>
                  <tr><th style="width:22%">নাম</th><td>{{ p.name }}</td><th style="width:16%">আইডি</th><td>{{ p.staffCode }}</td></tr>
                  <tr><th>পদবি</th><td>{{ p.designation }}</td><th>উপস্থিতি</th><td>{{ p.present | digits }} / {{ p.workDays | digits }} দিন</td></tr>
                </table>
                <table>
                  <tr><th>বিবরণ</th><th class="r">টাকা</th></tr>
                  <tr><td>মূল বেতন</td><td class="r">{{ p.basic | money }}</td></tr>
                  <tr><td>ভাতা</td><td class="r">{{ p.allowance | money }}</td></tr>
                  <tr><td>কর্তন</td><td class="r">− {{ p.deduction | money }}</td></tr>
                  <tr><th>মোট প্রদান</th><th class="r">{{ p.netPaid | money }}</th></tr>
                  <tr><td>প্রদানের তারিখ</td><td class="r">{{ p.paidOn | bdate }}@if (p.fund) { · {{ p.fund }} }</td></tr>
                </table>
                <div class="signs"><span>গ্রহণকারীর স্বাক্ষর</span><span>হিসাবরক্ষক</span><span>মুহতামিম</span></div>
              </div>
            }
          }
          @case ('report') {
            @let t = $any(j.data);
            <div class="doc">
              <m-doc-header />
              <h2>{{ t.title }}</h2>
              <p style="text-align:center;margin:-8px 0 10px;font-size:11pt">{{ t.subtitle }}</p>
              <table class="rep">
                <tr>@for (c of t.columns; track c.key) { <th [class.r]="isNum(c.kind)">{{ c.label }}</th> }</tr>
                @for (r of t.rows; track $index) { <tr>@for (c of t.columns; track c.key) { <td [class.r]="isNum(c.kind)">{{ cell(r[c.key], c.kind) }}</td> }</tr> }
                @if (t.totals) { <tr>@for (c of t.columns; track c.key) { <th [class.r]="isNum(c.kind)">{{ cell(t.totals[c.key], c.kind) }}</th> }</tr> }
              </table>
              <p style="font-size:9pt;color:#555">প্রিন্টের তারিখ: {{ today | bdate }}</p>
            </div>
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
  protected pad = (n: number) => String(n).padStart(6, '0');
  protected method = (m: keyof typeof METHOD_LABEL) => METHOD_LABEL[m];
  protected gender = (g: keyof typeof GENDER_LABEL) => GENDER_LABEL[g];
  protected photo = (p?: string) => fileUrl(p);
  protected today = todayIso();
  protected isNum = (k: string) => k === 'num' || k === 'money' || k === 'pct';
  private prefs = inject(PreferencesService);
  protected cell = (v: unknown, k: string) => reportCell(v, k, this.prefs.digits());
}
