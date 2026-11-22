import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { errorMessage } from '../../core/api';
import { NumPipe } from '../../core/format';
import { LookupsService } from '../../core/lookups.service';
import { ToastService } from '../../core/toast.service';
import { PrintService } from '../../shared/print/print.service';
import { Button, Card, Chips, EmptyState, Icon, PageHeader, Segmented, Skeleton } from '../../shared/ui';
import { StudentDetail, StudentRow } from '../students/student.models';
import { StudentsApi } from '../students/students.service';
import { IdCard } from './id-card';
import { TrPipe, tr } from '../../core/tr';

/** Pick a class (or arrive with ?ids= from the student list), preview, print 4 cards per A4 sheet. */
@Component({
  selector: 'app-idcards-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TrPipe, NumPipe, Button, Card, Chips, EmptyState, Icon, PageHeader, Segmented, Skeleton, IdCard],
  template: `
    <m-page-header [title]="'আইডি কার্ড' | tr" [description]="'কার্ডের QR কোড স্ক্যান করে হাজিরা নেওয়া যায়। এক A4 পাতায় ৪টি কার্ড ছাপা হয়।' | tr">
      <button mButton [disabled]="!picked().length" (click)="print()"><m-icon name="print" /> {{ '{n}টি কার্ড প্রিন্ট' | tr: { n: (picked().length | num) } }}</button>
    </m-page-header>

    <div class="flex flex-wrap items-center gap-3 mb-4">
      <m-chips [options]="classOptions()" [value]="classId()" (valueChange)="pickClass($event)" [attr.aria-label]="'শ্রেণি' | tr" />
      <m-segmented class="ml-auto" [options]="sides" [value]="side()" (valueChange)="side.set($event)" [attr.aria-label]="'কার্ডের দিক' | tr" />
    </div>

    @if (loading()) {
      <div class="grid gap-4 grid-cols-[repeat(auto-fill,minmax(330px,1fr))]">@for (i of [1,2,3]; track i) { <m-skeleton height="210px" /> }</div>
    } @else if (!students().length) {
      <m-card><m-empty-state [title]="'কোনো শিক্ষার্থী বাছাই করা হয়নি' | tr" [description]="'উপরে থেকে একটি শ্রেণি বেছে নিন, অথবা শিক্ষার্থী তালিকা থেকে কয়েকজনকে বাছাই করে ‘আইডি কার্ড’ চাপুন।' | tr" icon="card" /></m-card>
    } @else {
      <div class="flex items-center gap-3 mb-3 text-sm">
        <label class="inline-flex items-center gap-2 cursor-pointer"><input type="checkbox" class="size-4 accent-[var(--g2)]" [checked]="picked().length === students().length" (change)="toggleAll()"> {{ 'সবাই' | tr }}</label>
        <span class="text-mut">{{ '{a} / {b} জন বাছাই' | tr: { a: (picked().length | num), b: (students().length | num) } }}</span>
      </div>
      <div class="grid gap-5 grid-cols-[repeat(auto-fill,minmax(330px,1fr))]">
        @for (s of students(); track s.id) {
          <label class="grid gap-2 cursor-pointer">
            <span class="inline-flex items-center gap-2 text-sm"><input type="checkbox" class="size-4 accent-[var(--g2)]" [checked]="sel().has(s.id)" (change)="toggle(s.id)"> {{ s.name }}</span>
            <span [class.opacity-40]="!sel().has(s.id)"><app-id-card [s]="s" [side]="side()" /></span>
          </label>
        }
      </div>
    }
  `,
})
export class IdCardsPage {
  private api = inject(StudentsApi);
  private printer = inject(PrintService);
  private toast = inject(ToastService);
  protected lookups = inject(LookupsService);

  protected classId = signal<number | 0>(0);
  protected side = signal<'front' | 'back'>('front');
  protected sides = [{ value: 'front' as const, label: tr('সামনে') }, { value: 'back' as const, label: tr('পেছনে') }];
  protected students = signal<StudentDetail[]>([]);
  protected sel = signal(new Set<number>());
  protected loading = signal(false);
  protected picked = computed(() => this.students().filter((s) => this.sel().has(s.id)));
  protected classOptions = computed(() => [{ value: 0, label: tr('বাছাই করা') }, ...this.lookups.classes().map((c) => ({ value: c.id, label: c.nameBn }))]);

  constructor() {
    const ids = (inject(ActivatedRoute).snapshot.queryParamMap.get('ids') ?? '').split(',').map(Number).filter(Boolean);
    if (ids.length) this.loadDetails(ids);
  }

  protected async pickClass(id: number) {
    this.classId.set(id);
    if (!id) { this.students.set([]); return; }
    this.loading.set(true);
    try {
      const rows: StudentRow[] = (await this.api.all({ classId: id, status: 'Active' })).items;
      await this.loadDetails(rows.map((r) => r.id));
    } catch (e) { this.toast.error(errorMessage(e)); this.loading.set(false); }
  }

  /** The card needs guardians and photo, so fetch full details a few at a time. */
  private async loadDetails(ids: number[]) {
    this.loading.set(true);
    try {
      const out: StudentDetail[] = [];
      for (let i = 0; i < ids.length; i += 8) out.push(...(await Promise.all(ids.slice(i, i + 8).map((id) => this.api.get(id)))));
      out.sort((a, b) => (a.className ?? '').localeCompare(b.className ?? '') || (a.roll ?? 0) - (b.roll ?? 0));
      this.students.set(out);
      this.sel.set(new Set(out.map((s) => s.id)));
    } catch (e) { this.toast.error(errorMessage(e)); }
    finally { this.loading.set(false); }
  }

  protected toggle(id: number) { this.sel.update((s) => { const n = new Set(s); if (n.has(id)) n.delete(id); else n.add(id); return n; }); }
  protected toggleAll() { this.sel.set(this.picked().length === this.students().length ? new Set() : new Set(this.students().map((s) => s.id))); }
  protected print() { this.printer.print({ kind: 'idcards', data: this.picked() }); }
}
