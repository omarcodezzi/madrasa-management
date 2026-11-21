import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { errorMessage, fileUrl } from '../../core/api';
import { AuthService, P } from '../../core/auth/auth.service';
import { DigitsPipe, NumPipe } from '../../core/format';
import { LookupsService } from '../../core/lookups.service';
import { ToastService } from '../../core/toast.service';
import { Avatar, Badge, BulkActionsDef, Button, CellDef, Chips, Column, ConfirmService, DataTable, Icon, IconButton, PageHeader, RowActionsDef } from '../../shared/ui';
import { STATUS_LABEL, StudentRow } from './student.models';
import { StudentsApi } from './students.service';

@Component({
  selector: 'app-student-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, NumPipe, DigitsPipe, Avatar, Badge, Button, Chips, DataTable, CellDef, RowActionsDef, BulkActionsDef, Icon, IconButton, PageHeader],
  template: `
    <m-page-header title="শিক্ষার্থী" [description]="'মোট ' + (rows().length | num) + ' জন শিক্ষার্থী, ' + (lookups.classes().length | num) + 'টি শ্রেণি'">
      @if (canEdit()) { <a mButton routerLink="/admission"><m-icon name="plus" /> নতুন শিক্ষার্থী</a> }
    </m-page-header>

    @if (error()) {
      <div class="mb-4 flex items-center gap-3 px-4 py-3 rounded-xl bg-redbg text-red" role="alert">
        <m-icon name="alert" /><span class="flex-1">{{ error() }}</span>
        <button mButton size="sm" variant="outline" (click)="load()">আবার চেষ্টা করুন</button>
      </div>
    }

    <m-data-table [rows]="filtered()" [columns]="columns" [loading]="loading()" [selectable]="true" exportName="students"
      searchPlaceholder="নাম, আইডি, রোল বা মোবাইল দিয়ে খুঁজুন" emptyTitle="এখনো কোনো শিক্ষার্থী নেই" emptyText="প্রথম শিক্ষার্থীকে ভর্তি করে শুরু করুন।"
      (rowClick)="open($event)">
      <m-chips tableFilters [options]="classChips()" [value]="cls()" (valueChange)="cls.set($event)" />
      @if (canEdit()) { <a emptyAction mButton routerLink="/admission"><m-icon name="plus" /> নতুন ভর্তি</a> }

      <ng-template mCell="name" let-r>
        <div class="flex items-center gap-2.5">
          <m-avatar [name]="r.name" [src]="photo(r.photoPath)" [size]="36" />
          <div class="leading-tight"><b class="font-semibold">{{ r.name }}</b><small class="block text-mut text-[13px]">পিতা: {{ r.fatherName || '—' }}</small></div>
        </div>
      </ng-template>
      <ng-template mCell="roll" let-r>{{ r.roll | num }}</ng-template>
      <ng-template mCell="status" let-r>
        <m-badge [tone]="r.status === 'Active' ? 'success' : 'neutral'">{{ label(r.status) }}</m-badge>
        @if (r.isOrphan) { <m-badge tone="gold" class="ml-1">ইয়াতিম</m-badge> }
      </ng-template>
      <ng-template mCell="phone" let-r>@if (r.phone) { <a [href]="'tel:' + r.phone" (click)="$event.stopPropagation()" class="no-underline tabular">{{ r.phone | digits }}</a> }</ng-template>
      <ng-template mRowActions let-r>
        <div class="flex justify-end">
          <a mIconButton [routerLink]="['/students', r.id]" [attr.aria-label]="r.name + ' বিস্তারিত'"><m-icon name="chevronRight" [size]="18" /></a>
          @if (canEdit()) { <button mIconButton (click)="remove(r)" [attr.aria-label]="r.name + ' মুছুন'"><m-icon name="trash" [size]="18" /></button> }
        </div>
      </ng-template>
      <ng-template mBulkActions let-rows>
        <button mButton size="sm" variant="outline" (click)="idCards(rows)"><m-icon name="card" [size]="16" /> আইডি কার্ড</button>
      </ng-template>
    </m-data-table>
  `,
})
export class StudentListPage {
  private api = inject(StudentsApi);
  private router = inject(Router);
  private toast = inject(ToastService);
  private confirm = inject(ConfirmService);
  private auth = inject(AuthService);
  protected lookups = inject(LookupsService);

  protected rows = signal<StudentRow[]>([]);
  protected loading = signal(true);
  protected error = signal<string | null>(null);
  protected cls = signal<number | 0>(0);
  protected label = (s: string) => (STATUS_LABEL as Record<string, string>)[s] ?? s;
  protected canEdit = computed(() => this.auth.can(P.studentsEdit));

  protected classChips = computed(() => [
    { value: 0, label: `সবাই (${this.rows().length})` },
    ...this.lookups.classes()
      .map((c) => ({ c, n: this.rows().filter((r) => r.classId === c.id).length }))
      .filter((x) => x.n > 0)
      .map((x) => ({ value: x.c.id, label: `${x.c.nameBn} (${x.n})` })),
  ]);
  protected filtered = computed(() => (this.cls() ? this.rows().filter((r) => r.classId === this.cls()) : this.rows()));

  protected columns: Column<StudentRow>[] = [
    { key: 'name', label: 'শিক্ষার্থী', sortable: true, primary: true, value: (r) => `${r.name} ${r.fatherName ?? ''}` },
    { key: 'studentCode', label: 'আইডি', sortable: true },
    { key: 'className', label: 'শ্রেণি', sortable: true },
    { key: 'roll', label: 'রোল', sortable: true, align: 'end' },
    { key: 'phone', label: 'মোবাইল' },
    { key: 'status', label: 'অবস্থা', value: (r) => STATUS_LABEL[r.status] },
    { key: 'admissionDate', label: 'ভর্তির তারিখ', sortable: true, hidden: true },
  ];

  constructor() { this.load(); }

  async load() {
    this.loading.set(true);
    this.error.set(null);
    try { this.rows.set((await this.api.all()).items); }
    catch (e) { this.error.set(errorMessage(e)); }
    finally { this.loading.set(false); }
  }
  protected photo = (p?: string) => fileUrl(p);
  protected open(r: StudentRow) { this.router.navigate(['/students', r.id]); }
  protected idCards(rows: StudentRow[]) { this.router.navigate(['/idcards'], { queryParams: { ids: rows.map((r) => r.id).join(',') } }); }
  protected async remove(r: StudentRow) {
    if (!(await this.confirm.ask({ message: 'শিক্ষার্থীকে তালিকা থেকে সরানো হবে। পুরনো হাজিরা ও বেতনের হিসাব রিপোর্টে থেকে যাবে।', itemName: r.name }))) return;
    try {
      await this.api.remove(r.id);
      this.rows.update((l) => l.filter((x) => x.id !== r.id));
      this.toast.success(`${r.name} কে সরানো হয়েছে`);
    } catch (e) { this.toast.error(errorMessage(e)); }
  }
}
