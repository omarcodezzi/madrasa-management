import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { fileUrl } from '../../core/api';
import { DigitsPipe } from '../../core/format';
import { SessionService } from '../../core/session.service';

/** Madrasa letterhead used on every printed document. */
@Component({
  selector: 'm-doc-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DigitsPipe],
  template: `
    <div class="dhd">
      <img [src]="logo()" alt="">
      <div>
        <h1>{{ p()?.nameBn }}</h1>
        @if (p()?.nameAr) { <p style="font-family: Amiri, serif; direction: rtl; text-align: left">{{ p()?.nameAr }}</p> }
        <p>{{ p()?.address }}@if (p()?.phone) { · মোবাইল: {{ p()?.phone | digits }} }</p>
      </div>
    </div>
  `,
})
export class DocHeader {
  private session = inject(SessionService);
  protected p = this.session.profile;
  protected logo = computed(() => fileUrl(this.p()?.logoPath) ?? '/logo.jpg');
}
