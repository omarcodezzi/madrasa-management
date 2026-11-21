import { ChangeDetectionStrategy, Component, effect, input, signal } from '@angular/core';
import QRCode from 'qrcode';

/** Payload format printed on every ID card. Keep it unchanged so already printed cards keep working. */
export const studentQrPayload = (studentCode: string) => JSON.stringify({ type: 'RMTC_STUDENT', id: studentCode });

@Component({
  selector: 'm-qr',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `@if (src()) { <img [src]="src()" [alt]="alt()" class="block size-full" style="image-rendering: pixelated"> }`,
  host: { class: 'block' },
})
export class QrCode {
  readonly value = input.required<string>();
  readonly alt = input('QR');
  protected src = signal<string | null>(null);
  constructor() {
    effect(() => {
      const v = this.value();
      QRCode.toDataURL(v, { errorCorrectionLevel: 'M', margin: 1, width: 240 }).then((u) => this.src.set(u), () => this.src.set(null));
    });
  }
}
