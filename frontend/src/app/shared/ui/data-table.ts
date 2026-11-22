import {
  ChangeDetectionStrategy, Component, Directive, TemplateRef, computed, contentChild, contentChildren, inject, input, linkedSignal, output, signal,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DigitsPipe, NumPipe } from '../../core/format';
import { Icon } from './icon';
import { Skeleton } from './skeleton';
import { EmptyState } from './empty-state';
import { TrPipe, tr } from '../../core/tr';

export interface Column<T> {
  key: string;
  label: string;
  /** Value used for sorting, search and CSV. Defaults to row[key]. */
  value?: (row: T) => unknown;
  sortable?: boolean;
  align?: 'start' | 'end' | 'center';
  /** Hidden by default; user can turn it on from the column menu. */
  hidden?: boolean;
  /** Shown as the title line on the mobile card. */
  primary?: boolean;
}

/** <ng-template mCell="status" let-row>...</ng-template> */
@Directive({ selector: 'ng-template[mCell]' })
export class CellDef {
  readonly key = input.required<string>({ alias: 'mCell' });
  readonly tpl = inject(TemplateRef);
}
/** <ng-template mRowActions let-row>...</ng-template> → ⋯ column */
@Directive({ selector: 'ng-template[mRowActions]' })
export class RowActionsDef { readonly tpl = inject(TemplateRef); }
/** <ng-template mBulkActions let-rows>...</ng-template> → bar shown when rows are selected */
@Directive({ selector: 'ng-template[mBulkActions]' })
export class BulkActionsDef { readonly tpl = inject(TemplateRef); }

type SortDir = 'asc' | 'desc';

/**
 * The one table used on every page (spec 7): sticky header, sorting, search, column visibility,
 * pagination with page size, selection + bulk bar, row actions, density, CSV export, card view on phones.
 * Client-side for now; a server-paged mode is added with the Students module.
 */
@Component({
  selector: 'm-data-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TrPipe, NgTemplateOutlet, FormsModule, Icon, Skeleton, EmptyState, NumPipe, DigitsPipe],
  host: { class: 'block' },
  templateUrl: './data-table.html',
})
export class DataTable<T extends object> {
  readonly rows = input.required<T[]>();
  readonly columns = input.required<Column<T>[]>();
  readonly trackBy = input<(r: T) => unknown>((r: T) => (r as { id?: unknown }).id ?? r);
  readonly loading = input(false);
  readonly selectable = input(false);
  readonly searchable = input(true);
  readonly searchPlaceholder = input(tr('খুঁজুন…'));
  readonly exportName = input<string | null>('export');
  readonly emptyTitle = input(tr('কোনো তথ্য নেই'));
  readonly emptyText = input<string>();
  readonly pageSizes = input([10, 25, 50, 100]);
  readonly rowClick = output<T>();

  protected cells = contentChildren(CellDef);
  protected rowActions = contentChild(RowActionsDef);
  protected bulkActions = contentChild(BulkActionsDef);

  protected q = signal('');
  protected sortKey = signal<string | null>(null);
  protected sortDir = signal<SortDir>('asc');
  protected page = signal(1);
  protected pageSize = signal(10);
  protected dense = signal(false);
  protected colMenu = signal(false);
  protected hiddenCols = linkedSignal(() => new Set(this.columns().filter((c) => c.hidden).map((c) => c.key)));
  protected selected = signal(new Set<unknown>());

  protected visibleCols = computed(() => this.columns().filter((c) => !this.hiddenCols().has(c.key)));
  protected primaryCol = computed(() => this.visibleCols().find((c) => c.primary) ?? this.visibleCols()[0]);
  protected cellMap = computed(() => new Map(this.cells().map((c) => [c.key(), c.tpl])));

  protected val(row: T, c: Column<T>): unknown {
    return c.value ? c.value(row) : (row as Record<string, unknown>)[c.key];
  }

  protected filtered = computed(() => {
    const q = this.q().trim().toLowerCase();
    let list = this.rows();
    if (q) {
      const cols = this.columns();
      list = list.filter((r) => cols.some((c) => String(this.val(r, c) ?? '').toLowerCase().includes(q)));
    }
    const k = this.sortKey();
    if (k) {
      const c = this.columns().find((x) => x.key === k)!;
      const dir = this.sortDir() === 'asc' ? 1 : -1;
      list = [...list].sort((a, b) => {
        const va = this.val(a, c), vb = this.val(b, c);
        if (typeof va === 'number' && typeof vb === 'number') return (va - vb) * dir;
        return String(va ?? '').localeCompare(String(vb ?? ''), 'bn', { numeric: true }) * dir;
      });
    }
    return list;
  });
  protected pageCount = computed(() => Math.max(1, Math.ceil(this.filtered().length / this.pageSize())));
  protected pageRows = computed(() => {
    const p = Math.min(this.page(), this.pageCount());
    return this.filtered().slice((p - 1) * this.pageSize(), p * this.pageSize());
  });
  protected selectedRows = computed(() => {
    const s = this.selected(), t = this.trackBy();
    return this.rows().filter((r) => s.has(t(r)));
  });
  protected allOnPageSelected = computed(() => {
    const s = this.selected(), t = this.trackBy();
    return this.pageRows().length > 0 && this.pageRows().every((r) => s.has(t(r)));
  });

  protected setQuery(v: string) { this.q.set(v); this.page.set(1); }
  protected sort(c: Column<T>) {
    if (!c.sortable) return;
    if (this.sortKey() === c.key) this.sortDir.set(this.sortDir() === 'asc' ? 'desc' : 'asc');
    else { this.sortKey.set(c.key); this.sortDir.set('asc'); }
  }
  protected ariaSort(c: Column<T>) {
    return this.sortKey() === c.key ? (this.sortDir() === 'asc' ? 'ascending' : 'descending') : c.sortable ? 'none' : null;
  }
  protected toggleCol(key: string) {
    const s = new Set(this.hiddenCols());
    if (s.has(key)) s.delete(key); else if (this.visibleCols().length > 1) s.add(key);
    this.hiddenCols.set(s);
  }
  protected isSelected(r: T) { return this.selected().has(this.trackBy()(r)); }
  protected toggleRow(r: T) {
    const s = new Set(this.selected()), id = this.trackBy()(r);
    if (s.has(id)) s.delete(id); else s.add(id);
    this.selected.set(s);
  }
  protected togglePage() {
    const s = new Set(this.selected()), t = this.trackBy(), all = this.allOnPageSelected();
    this.pageRows().forEach((r) => (all ? s.delete(t(r)) : s.add(t(r))));
    this.selected.set(s);
  }
  clearSelection() { this.selected.set(new Set()); }

  protected exportCsv() {
    const cols = this.visibleCols();
    const esc = (v: unknown) => `"${String(v ?? '').replace(/"/g, '""')}"`;
    const lines = [cols.map((c) => esc(c.label)).join(','), ...this.filtered().map((r) => cols.map((c) => esc(this.val(r, c))).join(','))];
    // BOM so Excel opens Bangla correctly
    const blob = new Blob(['﻿' + lines.join('\r\n')], { type: 'text/csv;charset=utf-8' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = `${this.exportName()}.csv`;
    a.click();
    URL.revokeObjectURL(a.href);
  }
  protected alignCls(c: Column<T>) {
    return c.align === 'end' ? 'text-right tabular' : c.align === 'center' ? 'text-center' : 'text-left';
  }
}
