import { ChangeDetectionStrategy, Component, ElementRef, OnDestroy, output, signal, viewChild } from '@angular/core';
import { Button, Icon } from '../../shared/ui';

/** Camera QR reader (html5-qrcode, loaded on demand). Emits each decoded text once per 2.5 s. */
@Component({
  selector: 'app-qr-scanner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Icon],
  template: `
    <div class="relative aspect-square max-w-[360px] mx-auto rounded-2xl overflow-hidden bg-[#0b1f15] grid place-items-center text-white/80">
      <div #view id="qr-view" class="absolute inset-0 [&_video]:!w-full [&_video]:!h-full [&_video]:object-cover"></div>
      @if (!running()) {
        <div class="relative grid gap-3 justify-items-center p-6 text-center">
          <m-icon name="scan" [size]="44" />
          @if (error()) { <p class="m-0 text-sm text-[#ffd3cd]">{{ error() }}</p> } @else { <p class="m-0 text-sm">আইডি কার্ডের QR কোড ক্যামেরার সামনে ধরুন</p> }
          <button mButton variant="gold" (click)="start()" [loading]="starting()"><m-icon name="cam" /> ক্যামেরা চালু করুন</button>
        </div>
      } @else {
        <i class="absolute inset-[18%] border-2 border-[var(--gold)] rounded-2xl pointer-events-none shadow-[0_0_0_999px_rgba(0,0,0,.25)]"></i>
        <button mButton size="sm" variant="outline" class="absolute bottom-3 right-3 !bg-white/90" (click)="stop()"><m-icon name="x" [size]="16" /> বন্ধ</button>
      }
    </div>
  `,
})
export class QrScanner implements OnDestroy {
  readonly scanned = output<string>();
  protected running = signal(false);
  protected starting = signal(false);
  protected error = signal<string | null>(null);
  private view = viewChild.required<ElementRef<HTMLDivElement>>('view');
  private reader?: { start: Function; stop: () => Promise<void>; clear: () => void };
  private last = { text: '', at: 0 };

  async start() {
    this.starting.set(true); this.error.set(null);
    try {
      const { Html5Qrcode } = await import('html5-qrcode');
      this.reader ??= new Html5Qrcode(this.view().nativeElement.id, { verbose: false }) as never;
      await this.reader!.start({ facingMode: 'environment' }, { fps: 10, qrbox: (w: number, h: number) => { const s = Math.floor(Math.min(w, h) * 0.64); return { width: s, height: s }; } },
        (text: string) => this.onText(text), () => {});
      this.running.set(true);
    } catch (e) {
      const msg = String((e as Error)?.message ?? e);
      this.error.set(/Permission|NotAllowed/i.test(msg) ? 'ক্যামেরার অনুমতি দেওয়া হয়নি। ব্রাউজারের সেটিংস থেকে অনুমতি দিন, অথবা নিচে আইডি লিখুন।'
        : 'ক্যামেরা চালু করা যায়নি। নিচে আইডি লিখে হাজিরা দিন।');
    } finally { this.starting.set(false); }
  }
  async stop() { try { await this.reader?.stop(); } catch { /* already stopped */ } this.running.set(false); }
  private onText(text: string) {
    const now = Date.now();
    if (text === this.last.text && now - this.last.at < 2500) return;
    this.last = { text, at: now };
    this.scanned.emit(text);
  }
  ngOnDestroy() { if (this.running()) this.reader?.stop().catch(() => {}); }
}
