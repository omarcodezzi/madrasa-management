import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { fileUrl } from '../../core/api';
import { DigitsPipe } from '../../core/format';
import { SessionService } from '../../core/session.service';
import { QrCode, studentQrPayload } from '../../shared/ui';
import { StudentDetail } from '../students/student.models';
import { TrPipe } from '../../core/tr';

/** Front and back of the student ID card, same layout as the prototype. */
@Component({
  selector: 'app-id-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TrPipe, DigitsPipe, QrCode],
  host: { class: 'contents' },
  template: `
    @if (side() === 'front') {
      <div class="idc">
        <div class="ct">
          <img [src]="logo()" alt="">
          <div><b>{{ p()?.nameBn }}</b>@if (p()?.nameAr) {<i>{{ p()?.nameAr }}</i>}</div>
        </div>
        <div class="cb">
          <div class="pho">@if (photo()) { <img [src]="photo()" alt=""> } @else { {{ initial() }} }</div>
          <div class="cd">
            <b>{{ s().name }}</b>
            <span>{{ 'শ্রেণি:' | tr }}</span> {{ s().className }} &nbsp; <span>{{ 'রোল:' | tr }}</span> {{ s().roll | digits }}<br>
            <span>{{ 'আইডি:' | tr }}</span> <em>{{ s().studentCode }}</em><br>
            <span>{{ 'অভিভাবক:' | tr }}</span> {{ guardianPhone() | digits }}
          </div>
          <m-qr class="qr" [value]="qr()" [alt]="'QR ' + s().studentCode" />
        </div>
        <div class="cf"><span>{{ 'শিক্ষার্থী পরিচয়পত্র' | tr }}</span><span>{{ 'সেশন {s}' | tr: { s: (sessionName() | digits) } }}</span></div>
      </div>
    } @else {
      <div class="idc back">
        <div class="bb">
          <p><b>{{ 'পিতা:' | tr }}</b> {{ s().fatherName }}</p>
          <p><b>{{ 'ঠিকানা:' | tr }}</b> {{ s().presentAddress }}</p>
          <p><b>{{ 'জরুরি যোগাযোগ:' | tr }}</b> {{ guardianPhone() | digits }}</p>
          <p style="color:#5a6d60; font-size: .92em">{{ 'কার্ডটি পাওয়া গেলে অনুগ্রহ করে নিচের ঠিকানায় ফেরত দিন।' | tr }}<br>{{ p()?.nameBn }}, {{ p()?.phone | digits }}</p>
        </div>
        <div class="sig">{{ 'মুহতামিমের স্বাক্ষর' | tr }}</div>
        <div class="cf"><span>{{ shortAddress() }}</span></div>
      </div>
    }
  `,
})
export class IdCard {
  readonly s = input.required<StudentDetail>();
  readonly side = input<'front' | 'back'>('front');
  private session = inject(SessionService);
  protected p = this.session.profile;
  protected logo = computed(() => fileUrl(this.p()?.logoPath) ?? '/logo.jpg');
  protected photo = computed(() => fileUrl(this.s().photoPath));
  protected initial = computed(() => Array.from(this.s().name.replace(/^(মোঃ|মুহাম্মাদ)\s+/, ''))[0] ?? ''); // i18n-ignore
  protected qr = computed(() => studentQrPayload(this.s().studentCode));
  protected guardianPhone = computed(() => this.s().guardians.find((g) => g.isPrimary)?.phone ?? this.s().phone ?? '');
  protected sessionName = computed(() => this.session.current()?.name ?? '');
  protected shortAddress = computed(() => (this.p()?.address ?? '').split(',').slice(-2).join(',').trim());
}
